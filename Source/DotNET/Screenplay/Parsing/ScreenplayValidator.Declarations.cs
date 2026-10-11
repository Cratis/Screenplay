// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayValidator
{
    static void ValidateAuthorizations(ApplicationSyntax application, HashSet<string> knownPolicies, ParserContext context)
    {
        foreach (var module in application.Modules)
        {
            ValidateAuthorize(module.Authorize, knownPolicies, context);
            foreach (var feature in module.Features.SelectMany(AllFeatures))
            {
                ValidateAuthorize(feature.Authorize, knownPolicies, context);
            }
        }
    }

    static void ValidatePersonaPolicies(ApplicationSyntax application, HashSet<string> knownPolicies, bool allowUnresolvedPersonaPolicies, ParserContext context)
    {
        foreach (var persona in application.Personas ?? [])
        {
            foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))
            {
                if (allowUnresolvedPersonaPolicies)
                {
                    context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
                }
                else
                {
                    context.Error(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
                }
            }
        }
    }

    static void ValidateDuplicateProviders(ApplicationSyntax application, ParserContext context)
    {
        foreach (var duplicate in (application.Authentication?.Providers ?? [])
            .GroupBy(provider => provider.Identity, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateProvider, $"Duplicate provider '{duplicate.Identity}' - a provider must be distinguishable, so give one of them a name", duplicate.Location);
        }
    }

    static void ValidateSeeds(ApplicationSyntax application, HashSet<string> knownEvents, ParserContext context)
    {
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
    }

    static (List<Declaration> Declarations, Dictionary<Declaration, CommandSyntax> ByDeclaration) CommandDeclarations(IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> scopedSlices)
    {
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

        return (knownCommandDeclarations, commandsByDeclaration);
    }

    static void RunConsistencyValidators(ApplicationSyntax application, ConsistencyDeclarations declarations, EffectiveSpecificationApplication expansion, ParserContext context)
    {
        EventSourceValidator.Validate(application, declarations, context);
        SpecificationStreamValidator.Validate(application, declarations, context, expansion);
        OperationValidator.Validate(application, declarations, context);
        ImportValidator.Validate(application, declarations, context);
        CommandConsistencyValidator.Validate(declarations, context);
        CommandResponseValidator.Validate(application, declarations, context);
        SpecificationResponseValidator.Validate(application, declarations, context);
        EventFieldConsistencyValidator.Validate(declarations, context);
        ProjectionCompletenessValidator.Validate(declarations, context);
        ProjectionVariantValidator.Validate(declarations, context);
        ProjectionTargetValidator.Validate(declarations, context);
        IdentifierComplianceValidator.Validate(application, declarations, context);
        EventSubjectValidator.Validate(application, declarations, context);
        ReadModelKeyValidator.Validate(application, declarations, context);
        SpecificationValueConsistencyValidator.Validate(declarations, context);
        SpecificationOutcomeConsistencyValidator.Validate(declarations, context);
        SpecificationActionValidator.Validate(application, declarations, context);
        ReactionRefusalValidator.Validate(application, declarations, context);
        SpecificationRedeliveryValidator.Validate(application, declarations, context);
    }

    static void ValidateAdditionalEventReferences(ApplicationSyntax application, ConsistencyDeclarations declarations, HashSet<string> knownEvents, ParserContext context)
    {
        var events = declarations.Slices.SelectMany(entry => EventDeclarations.In(entry.Slice)
            .Select(@event => @event.Name).Distinct(StringComparer.Ordinal)
            .Select(name => new Declaration(name, entry.Scope))).ToArray();
        var imported = application.Imports.Select(import => import.QualifiedName).ToHashSet(StringComparer.Ordinal);
        foreach (var (slice, scope) in declarations.Slices)
        {
            bool IsKnown(string name) => knownEvents.Contains(name) || imported.Contains(name) || !ReferenceResolver.Resolve(name, scope, events).IsUnresolved;
            new RemovalEventValidator(IsKnown, context).VisitSlice(slice);
            foreach (var append in slice.Captures.SelectMany(capture => capture.Appends
                .Concat(capture.Children.SelectMany(children => children.Appends))
                .Concat(capture.Nested.SelectMany(nested => nested.Appends))))
            {
                if (!IsKnown(append.Event))
                {
                    context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{append.Event}' - declare it with 'event {append.Event}'", append.Location);
                }
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
        ReportDuplicateReadModels(slices, context);
        ReportReadModelsBuiltMoreThanOnce(slices, context);
        foreach (var reducer in slices.SelectMany(slice => slice.Reducers ?? []))
        {
            ValidateReducerReferences(reducer, knownReadModels, knownEvents, context);
        }
    }

    static void ReportDuplicateReadModels(List<SliceSyntax> slices, ParserContext context)
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
    }

    static void ReportReadModelsBuiltMoreThanOnce(List<SliceSyntax> slices, ParserContext context)
    {
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
    }

    static void ValidateReducerReferences(ReducerSyntax reducer, HashSet<string> knownReadModels, HashSet<string> knownEvents, ParserContext context)
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
                property.Type.Name == "optional"
                    ? $"Unknown type 'optional' on '{property.Name}' of {owner} - did you forget the type before 'optional'?"
                    : $"Unknown type '{property.Type.Name}' on '{property.Name}' of {owner} - declare it with 'concept {property.Type.Name} : <Primitive>' or 'type {property.Type.Name}'",
                property.Location);
        }
    }

    sealed class RemovalEventValidator(Func<string, bool> isKnown, ParserContext context) : ScreenplaySyntaxWalker
    {
        public override void VisitRemoveWith(RemoveWithSyntax syntax) => Validate(syntax.Event, syntax.Location);

        public override void VisitRemoveViaJoin(RemoveViaJoinSyntax syntax) => Validate(syntax.Event, syntax.Location);

        void Validate(string name, SourceLocation location)
        {
            if (!isKnown(name))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{name}' - declare it with 'event {name}'", location);
            }
        }
    }
}
