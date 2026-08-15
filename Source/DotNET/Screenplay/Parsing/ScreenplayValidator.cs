// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates cross references in a parsed document - policies referenced by <c>authorize</c> and personas,
/// events referenced by reactors, <c>produces</c>, constraints and <c>seed</c> blocks, and the types
/// referenced by properties - and that <c>concurrency</c> and <c>seed</c> blocks are not empty and
/// <c>authentication</c> provider and <c>type</c> names are unique.
/// </summary>
internal static class ScreenplayValidator
{
    /// <summary>
    /// Validates an application and reports warnings for unknown references.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var slices = application.Modules
            .SelectMany(module => module.Features.SelectMany(AllFeatures))
            .SelectMany(feature => feature.Slices)
            .ToList();

        var knownEvents = slices.SelectMany(slice => slice.Events.Select(@event => @event.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var knownPolicies = application.Policies.Select(policy => policy.Name).ToHashSet();

        // A read model is whatever a builder names with '=>', plus anything declared on its own.
        var knownReadModels = slices.SelectMany(slice => slice.Projections)
            .Select(projection => projection.ReadModel ?? projection.Name)
            .Concat(slices.SelectMany(slice => slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Concat(slices.SelectMany(slice => slice.ReadModels ?? []).Select(readModel => readModel.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();

        ValidateReadModels(slices, knownReadModels, knownEvents, context);
        var knownTypes = ConceptSyntax.PrimitiveTypes
            .Concat(application.Concepts.Select(concept => concept.Name))
            .Concat((application.Types ?? []).Select(type => type.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();

        ValidateTypes(application, knownTypes, context);

        foreach (var persona in application.Personas ?? [])
        {
            foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))
            {
                context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
            }
        }

        foreach (var duplicate in (application.Authentication?.Providers ?? [])
            .GroupBy(provider => provider.Identity, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateProvider, $"Duplicate provider '{duplicate.Identity}' - a provider must be distinguishable, so give one of them a name", duplicate.Location);
        }

        foreach (var seed in application.Seeds ?? [])
        {
            if (!seed.Groups.Any())
            {
                context.Error(DiagnosticCodes.EmptySeed, "Empty 'seed' block - declare at least one 'for' group", seed.Location);
            }

            foreach (var @event in seed.Groups.SelectMany(group => group.Events)
                .Where(@event => !knownEvents.Contains(@event.Event)))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{@event.Event}' - declare it with 'event {@event.Event}'", @event.Location);
            }
        }

        var knownCommands = slices.SelectMany(slice => slice.Commands.Select(command => command.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();

        foreach (var slice in slices)
        {
            ValidateSlice(slice, knownEvents, knownPolicies, knownTypes, knownReadModels, context);
            ValidateReactorConsequences(slice, knownEvents, knownCommands, context);
        }

        var scopedSlices = ScopedSlices(application).ToList();
        var knownQueries = scopedSlices.SelectMany(entry => entry.Slice.Queries.Select(query => new Declaration(query.Name, entry.Scope))).ToList();
        var knownScreenDeclarations = scopedSlices.SelectMany(entry => entry.Slice.Screens.Select(screen => new Declaration(screen.Name, entry.Scope))).ToList();

        var knownCommandDeclarations = new List<Declaration>();
        var commandsByDeclaration = new Dictionary<Declaration, CommandSyntax>();
        foreach (var entry in scopedSlices)
        {
            foreach (var command in entry.Slice.Commands)
            {
                var declaration = new Declaration(command.Name, entry.Scope);
                knownCommandDeclarations.Add(declaration);
                commandsByDeclaration[declaration] = command;
            }
        }

        ValidateScreenReferences(scopedSlices, knownQueries, knownCommandDeclarations, knownScreenDeclarations, context);
        ValidateFormReferences(application, commandsByDeclaration, knownQueries, knownCommandDeclarations, knownScreenDeclarations, context);
        ValidateContributions(application, knownScreenDeclarations, context);
        ValidateThemes(application, context);
        ValidateLayouts(application, context);
    }

    /// <summary>
    /// Validates that every <c>freeform</c> layout's <c>variant</c>s agree on which slots exist - a variant
    /// that omits a slot another variant of the same layout places (or explicitly hides) leaves that slot's
    /// presence undefined for the size class the omitting variant targets.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// Whether a <c>ui profile</c> targeting a size class with no matching <c>variant</c> should warn is a
    /// build-time concern - it depends on which layouts a profile's screens actually resolve to, which the
    /// Screenplay compiler does not know. That check belongs to Stage's build pipeline, not here.
    /// </remarks>
    static void ValidateLayouts(ApplicationSyntax application, ParserContext context)
    {
        foreach (var layout in application.Modules.SelectMany(module => module.Layouts).Where(layout => layout.Variants is not null))
        {
            var variants = layout.Variants!.ToList();
            var allSlots = variants.SelectMany(variant => variant.Places.Select(place => place.SlotName)).ToHashSet();

            foreach (var variant in variants)
            {
                var declared = variant.Places.Select(place => place.SlotName).ToHashSet();
                foreach (var missing in allSlots.Except(declared))
                {
                    context.Warning(
                        DiagnosticCodes.VariantMissingSlot,
                        $"Layout '{layout.Name}' variant for width {variant.Width}, height {variant.Height} does not mention slot '{missing}' - place it or declare it 'hidden'",
                        variant.Location);
                }
            }
        }
    }

    /// <summary>
    /// Validates that every <c>ui profile</c> selecting a theme names one the document declares, and that
    /// the theme is declared compatible with every package the profile itself lists.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// An arbitrary theme/package pairing can silently produce unstyled or broken components, so a profile
    /// selecting a theme not declared compatible with one of its own packages is reported - the pairing
    /// might still work by coincidence, but the gap is made visible the same way an unknown or ambiguous
    /// name already is.
    /// </remarks>
    static void ValidateThemes(ApplicationSyntax application, ParserContext context)
    {
        var themes = (application.Themes ?? []).ToDictionary(theme => theme.Name, StringComparer.Ordinal);

        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Theme is not { } themeName)
            {
                continue;
            }

            if (!themes.TryGetValue(themeName, out var theme))
            {
                context.Warning(DiagnosticCodes.UnknownTheme, $"Unknown theme '{themeName}' - declare it with 'theme {themeName}'", profile.Location);
                continue;
            }

            var compatibleWith = theme.CompatibleWith.ToHashSet(StringComparer.Ordinal);
            foreach (var package in profile.Packages.Where(package => !compatibleWith.Contains(package)))
            {
                context.Warning(
                    DiagnosticCodes.ThemeNotCompatibleWithPackage,
                    $"Theme '{themeName}' is not declared compatible with package '{package}' - components from that package may not receive {themeName}'s styling",
                    profile.Location);
            }
        }
    }

    /// <summary>
    /// Validates that a read model is declared once and built once.
    /// </summary>
    /// <param name="slices">Every <see cref="SliceSyntax"/> in the document.</param>
    /// <param name="knownReadModels">The read models the document makes available.</param>
    /// <param name="knownEvents">The events the document makes available.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A read model is built by exactly one thing - a projection or a reducer, never both and never two of
    /// either. Two builders would leave a reader, and a runtime, with no answer to which one produced the
    /// value in front of them.
    /// </remarks>
    static void ValidateReadModels(
        List<SliceSyntax> slices,
        HashSet<string> knownReadModels,
        HashSet<string> knownEvents,
        ParserContext context)
    {
        var declarations = slices.SelectMany(slice => slice.ReadModels ?? []).ToList();
        foreach (var duplicate in declarations.GroupBy(readModel => readModel.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(
                DiagnosticCodes.DuplicateReadModel,
                $"Duplicate read model '{duplicate.Name}' - a read model is declared once",
                duplicate.Location);
        }

        var builders = slices.SelectMany(slice => slice.Projections
                .Select(projection => (Name: projection.ReadModel ?? projection.Name, projection.Location)))
            .Concat(slices.SelectMany(slice => (slice.Reducers ?? [])
                .Select(reducer => (Name: reducer.ReadModel, reducer.Location))));

        foreach (var second in builders.GroupBy(builder => builder.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(
                DiagnosticCodes.ReadModelBuiltMoreThanOnce,
                $"Read model '{second.Name}' is built more than once - a projection or a reducer builds it, and only one of them",
                second.Location);
        }

        foreach (var reducer in slices.SelectMany(slice => slice.Reducers ?? []))
        {
            if (!knownReadModels.Contains(reducer.ReadModel))
            {
                context.Warning(
                    DiagnosticCodes.UnknownReadModel,
                    $"Unknown read model '{reducer.ReadModel}' - declare it with 'readmodel {reducer.ReadModel}'",
                    reducer.Location);
            }

            foreach (var rule in reducer.Rules.Where(rule => !knownEvents.Contains(rule.Event)))
            {
                context.Warning(
                    DiagnosticCodes.UnknownEvent,
                    $"Unknown event '{rule.Event}' - declare it with 'event {rule.Event}'",
                    rule.Location);
            }
        }
    }

    static IEnumerable<FeatureSyntax> AllFeatures(FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    static void ValidateTypes(ApplicationSyntax application, HashSet<string> knownTypes, ParserContext context)
    {
        var types = (application.Types ?? []).ToList();
        var declared = application.Concepts.Select(concept => concept.Name).Concat(types.Select(type => type.Name));

        foreach (var duplicate in declared.GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            var location = types.Find(type => type.Name == duplicate)?.Location ??
                           application.Concepts.First(concept => concept.Name == duplicate).Location;
            context.Error(DiagnosticCodes.DuplicateDeclaration, $"Duplicate declaration of '{duplicate}' - concept and type names must be unique", location);
        }

        foreach (var type in types)
        {
            ValidatePropertyTypes(type.Properties, $"type '{type.Name}'", knownTypes, context);
        }
    }

    /// <summary>
    /// Warns for every property whose type reference names nothing the document declares.
    /// </summary>
    /// <param name="properties">The <see cref="PropertySyntax">properties</see> to check.</param>
    /// <param name="owner">The declaration the properties belong to, used in diagnostics.</param>
    /// <param name="knownTypes">The primitives, concepts, types and imports the document makes available.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A reference to a shape that lives outside the document makes the document depend on something a
    /// reader cannot see. It stays a warning because a runtime may still resolve the name - what matters
    /// is that the gap is visible.
    /// </remarks>
    static void ValidatePropertyTypes(
        IEnumerable<PropertySyntax> properties,
        string owner,
        HashSet<string> knownTypes,
        ParserContext context)
    {
        foreach (var property in properties.Where(property => !knownTypes.Contains(property.Type.Name)))
        {
            context.Warning(
                DiagnosticCodes.UnknownType,
                $"Unknown type '{property.Type.Name}' on '{property.Name}' of {owner} - declare it with 'concept {property.Type.Name} : <Primitive>' or 'type {property.Type.Name}'",
                property.Location);
        }
    }

    /// <summary>
    /// Validates that what a command reads exists, and that the key it reads by is one of its own properties.
    /// </summary>
    /// <param name="command">The <see cref="CommandSyntax"/> to validate.</param>
    /// <param name="knownReadModels">The read models the document's projections produce.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateReads(CommandSyntax command, HashSet<string> knownReadModels, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();

        foreach (var reads in command.Reads ?? [])
        {
            if (!knownReadModels.Contains(reads.ReadModel))
            {
                context.Warning(
                    DiagnosticCodes.UnknownReadModel,
                    $"Unknown read model '{reads.ReadModel}' - no projection in the document produces it",
                    reads.Location);
            }

            if (reads.By is { } by && !properties.Contains(by))
            {
                context.Warning(
                    DiagnosticCodes.UnknownReadsKey,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' by '{by}', which is not one of its properties",
                    reads.Location);
            }
        }
    }

    /// <summary>
    /// Validates that the operands of a command's requirements name something the command can see.
    /// </summary>
    /// <param name="command">The <see cref="CommandSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A requirement is only worth stating if a consumer can tell what it is about. An operand is either a
    /// property of the command or a path into state the command declares it reads - anything else names
    /// something no reader of the document can resolve.
    /// </remarks>
    static void ValidateRequirements(CommandSyntax command, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();
        var reads = (command.Reads ?? []).Select(read => read.ReadModel).ToHashSet();

        foreach (var requirement in command.Validations.OfType<DeclarativeValidateSyntax>()
            .SelectMany(validate => validate.Requirements ?? []))
        {
            foreach (var operand in Operands(requirement.Condition))
            {
                var separator = operand.IndexOf('.', StringComparison.Ordinal);
                if (separator < 0)
                {
                    if (!properties.Contains(operand))
                    {
                        context.Warning(
                            DiagnosticCodes.UnknownRequirementOperand,
                            $"Command '{command.Name}' requires '{operand}', which is neither one of its properties nor state it reads",
                            requirement.Location);
                    }

                    continue;
                }

                var source = operand[..separator];
                if (!reads.Contains(source))
                {
                    context.Warning(
                        DiagnosticCodes.UnknownRequirementOperandSource,
                        $"Command '{command.Name}' requires '{operand}', but does not declare 'reads {source}'",
                        requirement.Location);
                }
            }
        }
    }

    /// <summary>
    /// Yields the left hand operand of every comparison in a condition.
    /// </summary>
    /// <param name="condition">The <see cref="ConditionSyntax"/> to walk.</param>
    /// <returns>The operands, in the order they appear.</returns>
    static IEnumerable<string> Operands(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => [comparison.Left],
        LogicalConditionSyntax logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };

    /// <summary>
    /// Validates that what a reactor sets off exists - the events it appends and the commands it invokes.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> holding the reactors.</param>
    /// <param name="knownEvents">The events the document makes available.</param>
    /// <param name="knownCommands">The commands the document declares.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateReactorConsequences(
        SliceSyntax slice,
        HashSet<string> knownEvents,
        HashSet<string> knownCommands,
        ParserContext context)
    {
        foreach (var trigger in slice.Reactors.SelectMany(reactor => reactor.Triggers))
        {
            foreach (var produces in (trigger.Produces ?? []).Where(produces => !knownEvents.Contains(produces.Event)))
            {
                context.Warning(
                    DiagnosticCodes.UnknownEvent,
                    $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'",
                    produces.Location);
            }

            foreach (var invokes in (trigger.Invokes ?? []).Where(invokes => !knownCommands.Contains(invokes.Command)))
            {
                context.Warning(
                    DiagnosticCodes.UnknownCommand,
                    $"Unknown command '{invokes.Command}' - declare it with 'command {invokes.Command}'",
                    invokes.Location);
            }
        }
    }

    /// <summary>
    /// Validates that what a screen binds to resolves - the queries it reads, the commands it invokes and
    /// the screens it navigates to.
    /// </summary>
    /// <param name="scoped">Every slice in the document, with the scope it sits in.</param>
    /// <param name="queries">The queries the document declares, scoped to where they are declared.</param>
    /// <param name="commands">The commands the document declares, scoped to where they are declared.</param>
    /// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A bare name resolves from the inside out - the slice, then the feature, then the module, then the
    /// document - and the innermost match wins. Reported as warnings rather than errors: a name may resolve
    /// to something outside the document, and the point is that the gap is visible.
    /// </remarks>
    static void ValidateScreenReferences(
        IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped,
        IReadOnlyList<Declaration> queries,
        IReadOnlyList<Declaration> commands,
        IReadOnlyList<Declaration> screens,
        ParserContext context)
    {
        foreach (var (slice, scope) in scoped)
        {
            foreach (var directive in slice.Screens.SelectMany(screen => AllDirectives(screen.Directives)))
            {
                switch (directive)
                {
                    case ScreenDataSyntax data:
                        Report(data.Query, scope, queries, DiagnosticCodes.UnknownQuery, "query", data.Location, context);
                        break;
                    case ScreenActionSyntax action:
                        Report(action.Command, scope, commands, DiagnosticCodes.UnknownCommand, "command", action.Location, context);
                        break;
                    case ScreenNavigateSyntax navigate:
                        Report(navigate.Screen, scope, screens, DiagnosticCodes.UnknownScreen, "screen", navigate.Location, context);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Validates that what a form binds to resolves - the command it submits, the fields it maps onto that
    /// command's properties, the query it populates from and the screen it navigates to on submit.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="commandsByDeclaration">Every declared command, keyed by its own declaration.</param>
    /// <param name="queries">The queries the document declares, scoped to where they are declared.</param>
    /// <param name="commands">The commands the document declares, scoped to where they are declared.</param>
    /// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A form sits at module level, not inside a slice, so it resolves against the module's own scope - it
    /// can disambiguate by module but not by feature or slice, since it does not sit inside either.
    /// </remarks>
    static void ValidateFormReferences(
        ApplicationSyntax application,
        Dictionary<Declaration, CommandSyntax> commandsByDeclaration,
        IReadOnlyList<Declaration> queries,
        IReadOnlyList<Declaration> commands,
        IReadOnlyList<Declaration> screens,
        ParserContext context)
    {
        foreach (var module in application.Modules)
        {
            var scope = new DeclarationScope([module.Name]);
            foreach (var form in module.Forms ?? [])
            {
                Report(form.For, scope, commands, DiagnosticCodes.UnknownCommand, "command", form.Location, context);

                var resolution = ReferenceResolver.Resolve(form.For, scope, commands);
                if (resolution.Resolved is { } resolved && commandsByDeclaration.TryGetValue(resolved, out var command))
                {
                    ValidateFormFields(form, command, context);
                }

                if (form.Populate is FormPopulateViaQuerySyntax populate)
                {
                    Report(populate.Query, scope, queries, DiagnosticCodes.UnknownQuery, "query", populate.Location, context);
                }

                if (form.OnSubmit is not null)
                {
                    Report(form.OnSubmit.Screen, scope, screens, DiagnosticCodes.UnknownScreen, "screen", form.OnSubmit.Location, context);
                }
            }
        }
    }

    /// <summary>
    /// Validates that every field a form declares binds to a property its command actually has - the same
    /// rename-breaks-the-build guarantee AutoMap gives a projection.
    /// </summary>
    /// <param name="form">The <see cref="FormSyntax"/> to validate.</param>
    /// <param name="command">The <see cref="CommandSyntax"/> the form binds to.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateFormFields(FormSyntax form, CommandSyntax command, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();
        foreach (var field in form.Fields.Where(field => !properties.Contains(field.Property)))
        {
            context.Warning(
                DiagnosticCodes.UnknownFormFieldProperty,
                $"Form '{form.Name}' has a field for '{field.Property}', which is not a property of command '{command.Name}'",
                field.Location);
        }
    }

    /// <summary>
    /// Validates that every contribution in the document resolves to a contribution point some layout
    /// template's slot declares, and that the screen it navigates to, if any, resolves.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A contribution resolves to the nearest enclosing template that declares a matching contribution
    /// point: first the contribution's own module - a module with its own matching slot stops contributions
    /// inside it from bubbling further out - then every other module in the document. This is a different
    /// algorithm from <see cref="ReferenceResolver"/>: it walks a physical containment tree (module owns
    /// layout owns slot) rather than narrowing by declaration-scope depth.
    /// </remarks>
    static void ValidateContributions(ApplicationSyntax application, IReadOnlyList<Declaration> screens, ParserContext context)
    {
        var offers = application.Modules
            .SelectMany(module => module.Layouts.SelectMany(layout => layout.Slots
                .Where(slot => slot.Contributes is not null)
                .Select(slot => (Module: module.Name, Layout: layout.Name, Slot: slot.Name, ContributionPoint: slot.Contributes!))))
            .ToList();

        foreach (var module in application.Modules)
        {
            var moduleScope = new DeclarationScope([module.Name]);
            foreach (var contribution in module.Contributions ?? [])
            {
                ValidateContribution(contribution, module.Name, moduleScope, offers, screens, context);
            }

            foreach (var (feature, scope) in AllFeaturesScoped(module.Features, [module.Name]))
            {
                foreach (var contribution in feature.Contributions ?? [])
                {
                    ValidateContribution(contribution, module.Name, scope, offers, screens, context);
                }
            }
        }
    }

    static void ValidateContribution(
        ContributionSyntax contribution,
        string ownModule,
        DeclarationScope scope,
        IReadOnlyList<(string Module, string Layout, string Slot, string ContributionPoint)> offers,
        IReadOnlyList<Declaration> screens,
        ParserContext context)
    {
        var ownModuleMatches = offers.Where(offer => offer.Module == ownModule && offer.ContributionPoint == contribution.ContributionPoint).ToList();
        if (ownModuleMatches.Count > 1)
        {
            var where = string.Join(", ", ownModuleMatches.Select(offer => $"{offer.Layout}.{offer.Slot}"));
            context.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Ambiguous contribution point '{contribution.ContributionPoint}' - {ownModuleMatches.Count} slots in module '{ownModule}' declare it ({where})",
                contribution.Location);
        }
        else if (ownModuleMatches.Count == 0)
        {
            var elsewhereMatches = offers.Where(offer => offer.Module != ownModule && offer.ContributionPoint == contribution.ContributionPoint)
                .Select(offer => offer.Module)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (elsewhereMatches.Count > 1)
            {
                context.Warning(
                    DiagnosticCodes.AmbiguousReference,
                    $"Ambiguous contribution point '{contribution.ContributionPoint}' - {elsewhereMatches.Count} modules declare it ({string.Join(", ", elsewhereMatches)})",
                    contribution.Location);
            }
            else if (elsewhereMatches.Count == 0)
            {
                context.Warning(
                    DiagnosticCodes.UnknownContributionPoint,
                    $"Unknown contribution point '{contribution.ContributionPoint}' - no layout template slot declares 'contributes {contribution.ContributionPoint}'",
                    contribution.Location);
            }
        }

        if (contribution.Navigate is not null)
        {
            Report(contribution.Navigate.Screen, scope, screens, DiagnosticCodes.UnknownScreen, "screen", contribution.Navigate.Location, context);
        }
    }

    /// <summary>
    /// Yields every feature in a module's feature tree with the scope it sits in, at any nesting depth.
    /// </summary>
    /// <param name="features">The top level <see cref="FeatureSyntax">features</see> to walk.</param>
    /// <param name="path">The scope path leading to <paramref name="features"/>, outermost first.</param>
    /// <returns>Each feature and where it sits.</returns>
    static IEnumerable<(FeatureSyntax Feature, DeclarationScope Scope)> AllFeaturesScoped(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path)
    {
        foreach (var feature in features)
        {
            List<string> featurePath = [.. path, feature.Name];
            yield return (feature, new DeclarationScope(featurePath));

            foreach (var nested in AllFeaturesScoped(feature.Features, featurePath))
            {
                yield return nested;
            }
        }
    }

    static void Report(
        string reference,
        DeclarationScope from,
        IReadOnlyList<Declaration> declarations,
        string unknownCode,
        string kind,
        SourceLocation location,
        ParserContext context)
    {
        var resolution = ReferenceResolver.Resolve(reference, from, declarations);
        if (resolution.Resolved is not null)
        {
            return;
        }

        if (resolution.Ambiguous.Count > 0)
        {
            var where = string.Join(", ", resolution.Ambiguous.Select(candidate => string.Join('.', candidate.Scope.Segments)));
            context.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Ambiguous {kind} '{reference}' - it matches {resolution.Ambiguous.Count} declarations equally well ({where}); qualify it to say which",
                location);
            return;
        }

        context.Warning(unknownCode, $"Unknown {kind} '{reference}' - nothing in scope declares it", location);
    }

    /// <summary>
    /// Yields every slice with the scope it sits in, outermost segment first.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to walk.</param>
    /// <returns>Each slice and where it sits.</returns>
    static IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> ScopedSlices(ApplicationSyntax application)
    {
        foreach (var module in application.Modules)
        {
            foreach (var entry in ScopedSlicesIn(module.Features, [module.Name]))
            {
                yield return entry;
            }
        }
    }

    static IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> ScopedSlicesIn(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path)
    {
        foreach (var feature in features)
        {
            List<string> featurePath = [.. path, feature.Name];
            foreach (var slice in feature.Slices)
            {
                yield return (slice, new DeclarationScope([.. featurePath, slice.Name]));
            }

            foreach (var nested in ScopedSlicesIn(feature.Features, featurePath))
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// Flattens a screen's directives, descending into the layouts, slots and sections that hold more.
    /// </summary>
    /// <param name="directives">The directives to flatten.</param>
    /// <returns>Every directive, including the ones nested inside another.</returns>
    static IEnumerable<ScreenDirectiveSyntax> AllDirectives(IEnumerable<ScreenDirectiveSyntax> directives)
    {
        foreach (var directive in directives)
        {
            yield return directive;

            var nested = directive switch
            {
                ScreenLayoutSyntax layout => layout.Slots.Cast<ScreenDirectiveSyntax>(),
                ScreenSlotSyntax slot => slot.Directives,
                ScreenSectionSyntax section => section.Directives,
                _ => []
            };

            foreach (var child in AllDirectives(nested))
            {
                yield return child;
            }

            // A navigate hangs off an action or a table row rather than standing on its own.
            if (directive is ScreenActionSyntax { Navigate: { } afterAction })
            {
                yield return afterAction;
            }

            if (directive is ScreenTableSyntax { RowClick: { } onRowClick })
            {
                yield return onRowClick;
            }
        }
    }

    static void ValidateSlice(
        SliceSyntax slice,
        HashSet<string> knownEvents,
        HashSet<string> knownPolicies,
        HashSet<string> knownTypes,
        HashSet<string> knownReadModels,
        ParserContext context)
    {
        foreach (var @event in slice.Events)
        {
            ValidatePropertyTypes(@event.Properties, $"event '{@event.Name}'", knownTypes, context);
        }

        foreach (var command in slice.Commands)
        {
            ValidatePropertyTypes(command.Properties, $"command '{command.Name}'", knownTypes, context);
            ValidateReads(command, knownReadModels, context);
            ValidateRequirements(command, context);
        }

        // The return type of a query names a read model, which no construct declares - only the
        // parameters resolve against the document's own types.
        foreach (var query in slice.Queries)
        {
            var parameters = (query.By is null ? [] : new[] { query.By }).Concat(query.Filters)
                .Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location));
            ValidatePropertyTypes(parameters, $"query '{query.Name}'", knownTypes, context);
        }

        foreach (var concurrency in slice.Commands.Select(command => command.Concurrency)
            .OfType<ConcurrencySyntax>()
            .Where(concurrency => concurrency is { EventSource: false, EventSourceType: null, EventStreamType: null, EventStreamId: null } &&
                !concurrency.EventTypes.Any()))
        {
            context.Error(DiagnosticCodes.EmptyConcurrency, "Empty 'concurrency' block - declare at least one of eventSource, sourceType, streamType, streamId or events", concurrency.Location);
        }

        foreach (var authorize in slice.Commands.Select(command => command.Authorize)
            .Concat(slice.Queries.Select(query => query.Authorize))
            .OfType<AuthorizeSyntax>())
        {
            foreach (var policy in authorize.References().Where(policy => !knownPolicies.Contains(policy.Name)))
            {
                context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy.Name}' - declare it with 'policy {policy.Name}'", policy.Location);
            }
        }

        foreach (var produces in slice.Commands.SelectMany(command => command.Produces)
            .Where(produces => !knownEvents.Contains(produces.Event)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'", produces.Location);
        }

        foreach (var trigger in slice.Reactors.SelectMany(reactor => reactor.Triggers)
            .Where(trigger => !knownEvents.Contains(trigger.Event)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{trigger.Event}' - declare it with 'event {trigger.Event}'", trigger.Location);
        }

        foreach (var constraint in slice.Constraints)
        {
            var @event = constraint switch
            {
                UniquePropertyConstraintSyntax unique => unique.Event,
                UniqueEventConstraintSyntax unique => unique.Event,
                _ => null
            };

            if (@event?.Length > 0 && !knownEvents.Contains(@event))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{@event}' - declare it with 'event {@event}'", constraint.Location);
            }
        }
    }
}
