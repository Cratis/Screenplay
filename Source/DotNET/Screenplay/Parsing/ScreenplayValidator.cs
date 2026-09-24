// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates cross references in a parsed document - policies referenced by <c>authorize</c> and personas,
/// events referenced by reactions, <c>produces</c>, constraints and <c>seed</c> blocks, and the types
/// referenced by properties - and that <c>concurrency</c> and <c>seed</c> blocks are not empty and
/// <c>authentication</c> provider and <c>type</c> names are unique.
/// </summary>
internal static class ScreenplayValidator
{
    // What an inline behavior has: only a named behavior declares parameters, so an operand inside an inline
    // one is always a reference to something declared rather than to a parameter.
    static readonly IReadOnlySet<string> _noParameters = new HashSet<string>(StringComparer.Ordinal);

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
        foreach (var module in application.Modules)
        {
            ValidateAuthorize(module.Authorize, knownPolicies, context);
            foreach (var feature in module.Features.SelectMany(AllFeatures))
            {
                ValidateAuthorize(feature.Authorize, knownPolicies, context);
            }
        }

        // A read model is whatever a builder names with '=>', plus anything declared on its own. A
        // projection with variants produces no read model of its own name - each variant is its own,
        // named by the variant instead.
        var knownReadModels = slices.SelectMany(slice => slice.Projections)
            .SelectMany(ProjectionBuiltReadModelNames)
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

        var declaredTriggers = (application.Triggers ?? []).ToDictionary(trigger => trigger.Name, StringComparer.Ordinal);
        var eventsByName = slices.SelectMany(slice => slice.Events)
            .GroupBy(@event => @event.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            ValidateSlice(slice, knownEvents, knownPolicies, knownTypes, knownReadModels, context);
            ValidateReactionConsequences(slice, knownEvents, knownCommands, context);
            ValidateReactionTriggers(slice, knownEvents, declaredTriggers, eventsByName, context);
        }

        var scopedSlices = ScopedSlices(application).ToList();
        var declarations = new ConsistencyDeclarations(application, scopedSlices);
        ImportValidator.Validate(application, declarations, context);
        CommandConsistencyValidator.Validate(declarations, context);
        EventFieldConsistencyValidator.Validate(declarations, context);
        ProjectionCompletenessValidator.Validate(declarations, context);
        ProjectionVariantValidator.Validate(declarations, context);
        SpecificationValueConsistencyValidator.Validate(declarations, context);
        SpecificationOutcomeConsistencyValidator.Validate(declarations, context);
        var knownQueries = scopedSlices.SelectMany(entry => entry.Slice.Queries.Select(query => new Declaration(query.Name, entry.Scope))).ToList();
        var knownScreenDeclarations = scopedSlices.SelectMany(entry => entry.Slice.Screens.Select(screen => new Declaration(screen.Name, entry.Scope))).ToList();
        ValidateSpecificationQueries(scopedSlices, knownQueries, context);

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
        ValidateInteractions(
            application,
            scopedSlices,
            new(
                knownCommandDeclarations,
                knownScreenDeclarations,
                knownQueries,
                application.Modules.SelectMany(module => module.DialogTemplates ?? []).Select(template => template.Name).ToHashSet(StringComparer.Ordinal),
                knownEvents,
                (application.Triggers ?? []).Select(trigger => trigger.Name).ToHashSet(StringComparer.Ordinal)),
            context);
        ValidateFormReferences(application, commandsByDeclaration, knownQueries, knownCommandDeclarations, knownScreenDeclarations, context);
        ValidateContributions(application, knownScreenDeclarations, context);
        ValidateThemes(application, context);
        ValidateProfileLayouts(application, context);
        ValidateArrangements(application, context);
    }

    /// <summary>
    /// Validates that the <c>variant</c>s of every <c>freeform</c> arrangement agree on which slots exist -
    /// a variant that omits a slot another variant of the same arrangement places (or explicitly hides)
    /// leaves that slot's presence undefined for the size class the omitting variant targets.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// Whether a <c>ui profile</c> targeting a size class with no matching <c>variant</c> should warn is a
    /// build-time concern - it depends on which templates a profile's screens actually resolve to, which the
    /// Screenplay compiler does not know. That check belongs to Stage's build pipeline, not here.
    /// </remarks>
    static void ValidateArrangements(ApplicationSyntax application, ParserContext context)
    {
        foreach (var (kind, name, arrangement) in Arrangements(application))
        {
            var variants = (arrangement.Variants ?? []).ToList();
            if (variants.Count == 0)
            {
                continue;
            }

            var allSlots = variants.SelectMany(variant => variant.Places.Select(place => place.SlotName)).ToHashSet();

            foreach (var variant in variants)
            {
                var declared = variant.Places.Select(place => place.SlotName).ToHashSet();
                foreach (var missing in allSlots.Except(declared))
                {
                    context.Warning(
                        DiagnosticCodes.VariantMissingSlot,
                        $"The {kind} '{name}' has a variant for width {variant.Width}, height {variant.Height} that does not mention slot '{missing}' - place it or declare it 'hidden'",
                        variant.Location);
                }
            }
        }
    }

    /// <summary>
    /// Yields every <c>arrangement</c> in the document with what declares it.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to walk.</param>
    /// <returns>The kind and name of each structure that arranges its slots, with its arrangement.</returns>
    static IEnumerable<(string Kind, string Name, ArrangementSyntax Arrangement)> Arrangements(ApplicationSyntax application)
    {
        foreach (var layout in application.Layouts ?? [])
        {
            if (layout.Arrangement is not null)
            {
                yield return ("layout", layout.Name, layout.Arrangement);
            }
        }

        foreach (var module in application.Modules)
        {
            foreach (var template in module.ScreenTemplates)
            {
                if (template.Arrangement is not null)
                {
                    yield return ("screen template", template.Name, template.Arrangement);
                }
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                if (template.Arrangement is not null)
                {
                    yield return ("dialog template", template.Name, template.Arrangement);
                }
            }
        }
    }

    /// <summary>
    /// Validates that every <c>ui profile</c> selecting a layout names one the document declares.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// Reported the same way an unknown theme is - a warning, because a name may still resolve to something
    /// outside the document, and the point is that the gap stays visible.
    /// </remarks>
    static void ValidateProfileLayouts(ApplicationSyntax application, ParserContext context)
    {
        var layouts = (application.Layouts ?? []).Select(layout => layout.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Layout is { } layoutName && !layouts.Contains(layoutName))
            {
                context.Warning(DiagnosticCodes.UnknownLayout, $"Unknown layout '{layoutName}' - declare it with 'layout {layoutName}'", profile.Location);
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
                .SelectMany(projection => ProjectionBuiltReadModelNames(projection).Select(name => (Name: name, projection.Location))))
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

    static void ValidateAuthorize(AuthorizeSyntax? authorize, HashSet<string> knownPolicies, ParserContext context)
    {
        if (authorize is null)
        {
            return;
        }

        foreach (var policy in authorize.References().Where(policy => !knownPolicies.Contains(policy.Name)))
        {
            context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy.Name}' - declare it with 'policy {policy.Name}'", policy.Location);
        }
    }

    static IEnumerable<FeatureSyntax> AllFeatures(FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    /// <summary>
    /// Gets the names of the read model(s) a projection builds. A projection with <c>variant</c> blocks builds
    /// one read model per variant, named after the variant, rather than one named after the projection itself -
    /// the projection's own name identifies the shared logical identity, not a read model.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> to inspect.</param>
    /// <returns>The read model name(s) the projection builds.</returns>
    static IEnumerable<string> ProjectionBuiltReadModelNames(ProjectionSyntax projection)
    {
        var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToList();
        return variants.Count > 0 ? variants.Select(variant => variant.Name) : [projection.ReadModel ?? projection.Name];
    }

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
        var properties = command.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var declarations = (command.Reads ?? []).ToList();
        var repeatedViews = declarations.GroupBy(read => read.ReadModel, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var aliases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reads in declarations)
        {
            if (repeatedViews.Contains(reads.ReadModel) && reads.Alias is null)
            {
                context.Error(
                    DiagnosticCodes.MissingReadsAlias,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' more than once; each read needs an alias",
                    reads.Location);
            }

            if (reads.Alias is { } alias)
            {
                if (!aliases.Add(alias))
                {
                    context.Error(
                        DiagnosticCodes.DuplicateReadsAlias,
                        $"Command '{command.Name}' already uses reads alias '{alias}'",
                        reads.Location);
                }

                if (properties.Contains(alias))
                {
                    context.Error(
                        DiagnosticCodes.ReadsAliasConflictsWithProperty,
                        $"Reads alias '{alias}' conflicts with a property of command '{command.Name}'",
                        reads.Location);
                }
            }

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
        var declarations = (command.Reads ?? []).ToList();
        var reads = declarations.Where(read => declarations.Count(other => other.ReadModel == read.ReadModel) == 1)
            .Select(read => read.ReadModel)
            .Concat(declarations.Select(read => read.Alias).OfType<string>())
            .ToHashSet(StringComparer.Ordinal);

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
    /// Validates that what sets a reaction off resolves, and that it only takes values the occurrence carries.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> holding the reactions.</param>
    /// <param name="knownEvents">The events the document makes available.</param>
    /// <param name="declaredTriggers">The <see cref="TriggerSyntax">triggers</see> the document declares, by name.</param>
    /// <param name="eventsByName">The events the document declares in full, by name.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A name resolves against three sets, in the order they are closest to the document: the events it
    /// declares or imports, the triggers it declares, and the triggers a consumer registered with the
    /// compiler. Only when all three miss is the name unknown - an integration's trigger has no declaration
    /// to find, and reporting it would punish the extensibility the registry exists for.
    /// <para>
    /// A reaction's <c>where</c> operands are deliberately left unchecked, unlike a command's <c>require</c>.
    /// The two look alike and are not: a command requires against its own properties and what it declares it
    /// reads, both of which the document states in full. A <c>where</c> narrows whatever set the reaction
    /// off, and that shape is often unknown - an imported event is a bare name, a registered trigger need not
    /// state its values, and a clock trigger has no modeled shape at all. A reaction may also carry several
    /// triggers, so an operand valid for one is invalid for another. Checking under those conditions would
    /// report correct documents, and a warning that fires on correct input is worse than no warning.
    /// </para>
    /// </remarks>
    static void ValidateReactionTriggers(
        SliceSyntax slice,
        HashSet<string> knownEvents,
        Dictionary<string, TriggerSyntax> declaredTriggers,
        Dictionary<string, EventSyntax> eventsByName,
        ParserContext context)
    {
        foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
        {
            if (trigger.Source is not NamedTriggerSourceSyntax named)
            {
                continue;
            }

            if (declaredTriggers.TryGetValue(named.Name, out var declared))
            {
                ValidateTriggerData(trigger, declared.Data.Select(datum => datum.Name), named.Name, context);
                continue;
            }

            if (!knownEvents.Contains(named.Name))
            {
                if (!context.Languages.Triggers.TryGetValue(named.Name, out var registered))
                {
                    context.Warning(
                        DiagnosticCodes.UnknownTrigger,
                        $"Unknown trigger '{named.Name}' - declare it with 'event {named.Name}' or 'trigger {named.Name}'",
                        named.Location);
                    continue;
                }

                // A registration that states no shape is not a registration that states an empty one. The
                // first leaves the values alone; the second says an occurrence carries nothing, and taking
                // something from it is worth reporting.
                if (registered.Values is { } values)
                {
                    ValidateTriggerData(trigger, values, named.Name, context);
                }

                continue;
            }

            // An imported event is a name and nothing more, so there is no property list to check against and
            // taking a value from it says only what the author intends.
            if (eventsByName.TryGetValue(named.Name, out var @event))
            {
                ValidateTriggerData(trigger, @event.Properties.Select(property => property.Name), named.Name, context);
            }
        }
    }

    static void ValidateTriggerData(ReactionTriggerSyntax trigger, IEnumerable<string> available, string source, ParserContext context)
    {
        var names = available.ToHashSet(StringComparer.Ordinal);
        foreach (var datum in trigger.Data.Where(datum => !names.Contains(datum.Name)))
        {
            context.Warning(
                DiagnosticCodes.UnknownTriggerData,
                $"'{source}' carries no '{datum.Name}' - a reaction can only take values the occurrence provides",
                datum.Location);
        }
    }

    /// <summary>
    /// Validates that what a reaction sets off exists - the events it appends and the commands it invokes.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> holding the reactions.</param>
    /// <param name="knownEvents">The events the document makes available.</param>
    /// <param name="knownCommands">The commands the document declares.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateReactionConsequences(
        SliceSyntax slice,
        HashSet<string> knownEvents,
        HashSet<string> knownCommands,
        ParserContext context)
    {
        foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
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
    /// Validates that every query asserted by a specification resolves from the specification's slice.
    /// </summary>
    /// <param name="scoped">Every slice in the document, with the scope it sits in.</param>
    /// <param name="queries">The queries the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateSpecificationQueries(
        IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped,
        IReadOnlyList<Declaration> queries,
        ParserContext context)
    {
        foreach (var (slice, scope) in scoped)
        {
            foreach (var query in slice.Specifications.SelectMany(specification => specification.ThenQueries))
            {
                Report(query.Query, scope, queries, DiagnosticCodes.UnknownQuery, "query", query.Location, context);
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
    /// Validates that every operand an interaction names resolves - the command an action executes, the screen
    /// it navigates to, the query it refreshes, the dialog it opens, the trigger it raises or observes, and the
    /// behavior a <c>uses</c> clause attaches, along with that behavior's parameters.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="scoped">Every slice with the scope it sits in.</param>
    /// <param name="references">The declarations an action operand can resolve against.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// This is what makes an action a modeled reference rather than a string: an operand naming nothing is
    /// reported here, at compile time, instead of becoming a control that does nothing when clicked.
    /// </remarks>
    static void ValidateInteractions(
        ApplicationSyntax application,
        IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped,
        InteractionReferences references,
        ParserContext context)
    {
        var behaviors = application.Behaviors.ToDictionary(behavior => behavior.Name ?? string.Empty, StringComparer.Ordinal);
        var root = new DeclarationScope([]);

        // A named behavior is declared at the top level, so it resolves against the document as a whole.
        foreach (var behavior in application.Behaviors)
        {
            ValidateBindings(behavior.Bindings, root, references, behavior.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal), context);
        }

        // Structural attachments - a layout, a template, a module, a feature, a form. Each resolves from the
        // scope it sits in, the same as a screen's own attachment does.
        foreach (var layout in application.Layouts ?? [])
        {
            ValidateAttachments(layout.Behaviors, layout.UsedBehaviors, root, references, behaviors, context);
        }

        foreach (var module in application.Modules)
        {
            var moduleScope = new DeclarationScope([module.Name]);
            ValidateAttachments(module.Behaviors, module.UsedBehaviors, moduleScope, references, behaviors, context);

            foreach (var template in module.ScreenTemplates)
            {
                ValidateAttachments(template.Behaviors, template.UsedBehaviors, moduleScope, references, behaviors, context);
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                ValidateAttachments(template.Behaviors, template.UsedBehaviors, moduleScope, references, behaviors, context);
            }

            foreach (var form in module.Forms ?? [])
            {
                ValidateAttachments(form.Behaviors, form.UsedBehaviors, moduleScope, references, behaviors, context);
            }

            // Scoped from the module down, so a feature's attachment resolves the way a slice inside it does.
            foreach (var (feature, scope) in AllFeaturesScoped(module.Features, [module.Name]))
            {
                ValidateAttachments(feature.Behaviors, feature.UsedBehaviors, scope, references, behaviors, context);
            }
        }

        foreach (var (slice, scope) in scoped)
        {
            foreach (var directive in slice.Screens.SelectMany(screen => AllDirectives(screen.Directives)))
            {
                switch (directive)
                {
                    case ScreenBehaviorSyntax attached:
                        ValidateBindings(attached.Behavior.Bindings, scope, references, _noParameters, context);
                        break;
                    case ScreenUsesBehaviorSyntax uses:
                        ValidateUses(uses.Uses, behaviors, context);
                        break;
                    case ScreenTableSyntax table:
                        foreach (var behavior in table.Behaviors)
                        {
                            ValidateBindings(behavior.Bindings, scope, references, _noParameters, context);
                        }

                        foreach (var uses in table.UsedBehaviors)
                        {
                            ValidateUses(uses, behaviors, context);
                        }

                        break;
                }
            }
        }
    }

    static void ValidateAttachments(
        IEnumerable<BehaviorSyntax> attached,
        IEnumerable<UsesBehaviorSyntax> used,
        DeclarationScope scope,
        InteractionReferences references,
        Dictionary<string, BehaviorSyntax> behaviors,
        ParserContext context)
    {
        foreach (var behavior in attached)
        {
            ValidateBindings(behavior.Bindings, scope, references, _noParameters, context);
        }

        foreach (var uses in used)
        {
            ValidateUses(uses, behaviors, context);
        }
    }

    static void ValidateUses(UsesBehaviorSyntax uses, Dictionary<string, BehaviorSyntax> behaviors, ParserContext context)
    {
        if (!behaviors.TryGetValue(uses.Behavior, out var behavior))
        {
            context.Warning(
                DiagnosticCodes.UnknownUsedBehavior,
                $"Unknown behavior '{uses.Behavior}' - nothing in the document declares it",
                uses.Location);
            return;
        }

        var declared = behavior.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
        var supplied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var argument in uses.Arguments)
        {
            supplied.Add(argument.Name);
            if (!declared.Contains(argument.Name))
            {
                context.Error(
                    DiagnosticCodes.UnknownBehaviorArgument,
                    $"Behavior '{behavior.Name}' declares no parameter '{argument.Name}'",
                    argument.Location);
            }
        }

        foreach (var missing in declared.Where(parameter => !supplied.Contains(parameter)).Order(StringComparer.Ordinal))
        {
            context.Error(
                DiagnosticCodes.MissingBehaviorArgument,
                $"Behavior '{behavior.Name}' declares a parameter '{missing}' this attachment supplies no value for",
                uses.Location);
        }
    }

    static void ValidateBindings(
        IEnumerable<InteractionBindingSyntax> bindings,
        DeclarationScope scope,
        InteractionReferences references,
        IReadOnlySet<string> parameters,
        ParserContext context)
    {
        foreach (var binding in bindings)
        {
            switch (binding.Trigger)
            {
                case EventInteractionTriggerSyntax @event when !references.Events.Contains(@event.EventName):
                    context.Warning(
                        DiagnosticCodes.UnknownInteractionEvent,
                        $"Unknown event '{@event.EventName}' - nothing in scope declares it",
                        @event.Location);
                    break;
                case ApplicationTriggerInteractionTriggerSyntax trigger when !references.Triggers.Contains(trigger.TriggerName):
                    context.Warning(
                        DiagnosticCodes.UnknownActionTrigger,
                        $"Unknown trigger '{trigger.TriggerName}' - nothing in the document declares it, and it is not a built-in interaction kind",
                        trigger.Location);
                    break;
            }

            ValidateActions(binding.Actions, scope, references, parameters, context);
        }
    }

    static void ValidateActions(
        IEnumerable<InteractionActionSyntax> actions,
        DeclarationScope scope,
        InteractionReferences references,
        IReadOnlySet<string> parameters,
        ParserContext context)
    {
        var ordered = actions.ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var action = ordered[index];

            // An operand may be the name of a parameter the attachment supplies rather than a declaration -
            // a behavior exists precisely so the same actions can point at different commands.
            switch (action)
            {
                case ExecuteCommandActionSyntax execute when !parameters.Contains(execute.Command):
                    Report(execute.Command, scope, references.Commands, DiagnosticCodes.UnknownActionCommand, "command", execute.Location, context);
                    break;
                case NavigateActionSyntax navigate when !parameters.Contains(navigate.Screen):
                    Report(navigate.Screen, scope, references.Screens, DiagnosticCodes.UnknownActionScreen, "screen", navigate.Location, context);
                    break;
                case RefreshQueryActionSyntax refresh when !parameters.Contains(refresh.Query):
                    Report(refresh.Query, scope, references.Queries, DiagnosticCodes.UnknownActionQuery, "query", refresh.Location, context);
                    break;
                case OpenDialogActionSyntax open when !parameters.Contains(open.DialogTemplate) && !references.DialogTemplates.Contains(open.DialogTemplate):
                    context.Warning(
                        DiagnosticCodes.UnknownActionDialogTemplate,
                        $"Unknown dialog template '{open.DialogTemplate}' - no module in the document declares it",
                        open.Location);
                    break;
                case RaiseTriggerActionSyntax raise when !parameters.Contains(raise.Trigger) && !references.Triggers.Contains(raise.Trigger):
                    context.Warning(
                        DiagnosticCodes.UnknownActionTrigger,
                        $"Unknown trigger '{raise.Trigger}' - nothing in the document declares it",
                        raise.Location);
                    break;
            }

            // Navigating replaces the screen the interaction was running on, so whatever was written after it
            // never runs. Reported rather than accepted, because the author expects those actions to happen.
            if (action is NavigateActionSyntax or NavigateBackActionSyntax && index < ordered.Count - 1)
            {
                context.Warning(
                    DiagnosticCodes.UnreachableInteractionContinuation,
                    "Actions after an unconditional navigation never run - the screen they were written for is already gone",
                    ordered[index + 1].Location);
            }

            ValidateActions(action.OnSuccess, scope, references, parameters, context);
            ValidateActions(action.OnFailure, scope, references, parameters, context);
            ValidateActions(action.OnResult, scope, references, parameters, context);
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
    /// Validates that every contribution in the document resolves to a contribution point some slot of the
    /// application's layout or of a module's template declares, and that the screen it navigates to, if any, resolves.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A contribution resolves to the nearest enclosing structure that declares a matching contribution
    /// point: first the contribution's own module - a module with its own matching slot stops contributions
    /// inside it from bubbling further out - then every other module in the document, and finally the
    /// application's own layouts, which belong to no module and are the outermost shell of all. This is a
    /// different algorithm from <see cref="ReferenceResolver"/>: it walks a physical containment tree
    /// (module owns template owns slot) rather than narrowing by declaration-scope depth.
    /// </remarks>
    static void ValidateContributions(ApplicationSyntax application, IReadOnlyList<Declaration> screens, ParserContext context)
    {
        var offers = application.Modules
            .SelectMany(module => Structures(module).SelectMany(structure => structure.Slots
                .Where(slot => slot.Contributes is not null)
                .Select(slot => (Module: module.Name, Structure: structure.Name, Slot: slot.Name, ContributionPoint: slot.Contributes!))))
            .Concat((application.Layouts ?? []).SelectMany(layout => layout.Slots
                .Where(slot => slot.Contributes is not null)
                .Select(slot => (Module: string.Empty, Structure: layout.Name, Slot: slot.Name, ContributionPoint: slot.Contributes!))))
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
        IReadOnlyList<(string Module, string Structure, string Slot, string ContributionPoint)> offers,
        IReadOnlyList<Declaration> screens,
        ParserContext context)
    {
        var ownModuleMatches = offers.Where(offer => offer.Module == ownModule && offer.ContributionPoint == contribution.ContributionPoint).ToList();
        if (ownModuleMatches.Count > 1)
        {
            var where = string.Join(", ", ownModuleMatches.Select(offer => $"{offer.Structure}.{offer.Slot}"));
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
                    $"Unknown contribution point '{contribution.ContributionPoint}' - no layout or template slot declares 'contributes {contribution.ContributionPoint}'",
                    contribution.Location);
            }
        }

        if (contribution.Navigate is not null)
        {
            Report(contribution.Navigate.Screen, scope, screens, DiagnosticCodes.UnknownScreen, "screen", contribution.Navigate.Location, context);
        }
    }

    /// <summary>
    /// Yields the slot bearing structures a module declares - its screen templates and its dialog templates.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> to read.</param>
    /// <returns>The name and slots of each.</returns>
    static IEnumerable<(string Name, IEnumerable<SlotSyntax> Slots)> Structures(ModuleSyntax module) =>
        module.ScreenTemplates.Select(template => (template.Name, template.Slots))
            .Concat((module.DialogTemplates ?? []).Select(template => (template.Name, template.Slots)));

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
    /// Flattens a screen's directives, descending into the templates, slots and sections that hold more.
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
                ScreenTemplateReferenceSyntax template => template.Slots.Cast<ScreenDirectiveSyntax>(),
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
            .Concat(slice.Queries.Select(query => query.Authorize)))
        {
            ValidateAuthorize(authorize, knownPolicies, context);
        }

        foreach (var produces in slice.Commands.SelectMany(command => command.Produces)
            .Where(produces => !knownEvents.Contains(produces.Event)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'", produces.Location);
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
