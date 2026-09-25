// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>Derives wrapper contracts from bound identities, never from attachment text or display-name joins.</summary>
internal static class SemanticTypedContextCatalog
{
    internal static ImmutableArray<SemanticTypedContextDescriptor> Create(
        SemanticApplication application,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        bool strict) => Build(application, requirements, strict);

    static SemanticContextType Runtime(string token) => new("runtime", null, null, token);
    static SemanticContextType Shape(SemanticId id) => new("shape", null, id, null);
    static SemanticContextType Model(SemanticTypeReference type)
    {
        if (type.Kind == SemanticTypeReferenceKind.Unknown ||
            (type.Kind == SemanticTypeReferenceKind.Primitive && type.Primitive == SemanticPrimitiveType.Unknown) ||
            (type.Kind is SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType && type.Target == default))
        {
            throw new InvalidSemanticContract("Typed context has an unresolved model type.");
        }

        return new("model", type, null, null);
    }

    static SemanticTypedContextMember Fixed(string name, string token) =>
        new(name, Runtime(token), false, false, new("context-contract", null, name));

    static SemanticTypedContextMember Derived(string name, string token, string from) =>
        new(name, Runtime(token), false, true, new("derived", null, from));

    static SemanticTypedContextMember Shaped(string name, SemanticId id, string kind, IEnumerable<SemanticProperty> properties, bool nullable = false) =>
        new(name, Shape(id) with { Properties = [.. properties.Select(value => new SemanticContextProperty(value.Name, value.Id, Model(value.Type).ModelType!))] }, nullable, false, new(kind, id, name));

    static SemanticTypedContextMember Typed(string name, SemanticTypeReference type, SemanticId id, string path) =>
        new(name, Model(type), type.IsOptional, false, new("model-property", id, path));

    static SemanticTypedContextDescriptor Descriptor(SemanticImplementationRequirement requirement, SemanticId? operation, params SemanticTypedContextMember[] members) =>
        new(requirement.RequirementId, requirement.Role, requirement.ContextVersion, operation, [.. members]);

    static ImmutableArray<SemanticTypedContextDescriptor> Build(
        SemanticApplication application,
        ImmutableArray<SemanticImplementationRequirement> requirements,
        bool strict)
    {
        var slices = application.Modules.SelectMany(AllSlices).ToArray();
        var commands = slices.SelectMany(slice => slice.Commands).ToArray();
        var queries = slices.SelectMany(slice => slice.Queries).ToArray();
        var events = slices.SelectMany(slice => slice.Events).ToArray();
        var readModels = slices.SelectMany(slice => slice.ReadModels).ToArray();
        var result = ImmutableArray.CreateBuilder<SemanticTypedContextDescriptor>();
        foreach (var requirement in requirements)
        {
            var matches = new List<SemanticTypedContextDescriptor>();
            switch (requirement.Role)
            {
                case SemanticImplementationRole.CommandHandler:
                    foreach (var command in commands.Where(value => value.Id == requirement.Source.SemanticId))
                    {
                        matches.Add(Descriptor(
                            requirement,
                            command.Id,
                            Shaped("Command", command.Id, "command", command.Properties),
                            Fixed("Tenant", "TenantId"),
                            Fixed("Identity", "Identity"),
                            Fixed("CausedBy", "CausedBy"),
                            Fixed("Causation", "Causation"),
                            Fixed("Occurred", "DateTimeOffset")));
                    }
                    break;
                case SemanticImplementationRole.CommandValidation:
                case SemanticImplementationRole.ConceptValidation:
                case SemanticImplementationRole.RulePredicate:
                    foreach (var command in commands.Where(value => value.Id == requirement.Source.SemanticId))
                    {
                        var rules = command.Validations.Where(value => value.RequirementId == requirement.RequirementId).ToArray();
                        var whole = command.CodeValidations.Any(value => value.RequirementId == requirement.RequirementId);
                        if (whole)
                        {
                            matches.Add(Rule(requirement, Shaped("Artifact", command.Id, "command", command.Properties), Shaped("Value", command.Id, "command", command.Properties), string.Empty));
                        }
                        foreach (var rule in rules)
                        {
                            var property = command.Properties.SingleOrDefault(value => value.Id == rule.Property);
                            if (property is not null)
                            {
                                matches.Add(Rule(requirement, Shaped("Artifact", command.Id, "command", command.Properties), Typed("Value", property.Type, property.Id, property.Name), property.Name));
                            }
                        }
                    }
                    foreach (var concept in application.Concepts.Where(value => value.Id == requirement.Source.SemanticId))
                    {
                        if (concept.Validations.Any(value => value.RequirementId == requirement.RequirementId))
                        {
                            var type = SemanticTypeReference.ForConcept(concept.Id);
                            matches.Add(Rule(
                                requirement,
                                Typed("Artifact", type, concept.Id, "value"),
                                Typed("Value", type, concept.Id, "value"),
                                string.Empty));
                        }
                    }
                    break;
                case SemanticImplementationRole.ReducerTransition:
                    foreach (var reducer in slices.SelectMany(slice => slice.Reducers))
                    {
                        foreach (var transition in reducer.Transitions.Where(value => value.RequirementId == requirement.RequirementId))
                        {
                            var state = readModels.SingleOrDefault(value => value.Id == reducer.ReadModel);
                            var @event = events.SingleOrDefault(value => value.Id == transition.EventContract);
                            if (state is null || @event is null) continue;
                            matches.Add(Descriptor(
                                requirement,
                                reducer.ReadModel,
                                Shaped("State", state.Id, "read-model", state.Properties, true),
                                Shaped("Event", @event.Id, "current-event", @event.Properties),
                                new("Key", Runtime("string"), false, false, new("event-source-id", @event.Id, "eventSourceId")),
                                Fixed("Tenant", "TenantId"),
                                Fixed("Occurred", "DateTimeOffset"),
                                Fixed("SequenceNumber", "long"),
                                Derived("IsFirst", "bool", "State")));
                        }
                    }
                    break;
                case SemanticImplementationRole.PolicyPredicate:
                    var policyNames = application.Policies.Where(value => value.Condition is SemanticOpaquePolicyCondition opaque && opaque.RequirementId == requirement.RequirementId)
                        .Select(value => value.Name).ToHashSet(StringComparer.Ordinal);
                    foreach (var command in commands.Where(value => References(value.Authorization, policyNames)))
                    {
                        var identifier = command.Properties.SingleOrDefault(value => value.IsIdentifier);
                        var subject = identifier is null
                            ? new SemanticContextSource("context-contract", null, "Subject")
                            : new SemanticContextSource("command-identifier", identifier.Id, identifier.Name);
                        matches.Add(Policy(requirement, command.Id, command.Properties, subject));
                    }
                    foreach (var query in queries.Where(value => References(value.Authorization, policyNames)))
                    {
                        matches.Add(Policy(
                            requirement,
                            query.Id,
                            [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)],
                            new SemanticContextSource("query-key", query.Argument.Id, query.Argument.Name)));
                    }
                    break;
            }

            if (strict && matches.Count == 0 && requirement.Role is SemanticImplementationRole.CommandHandler or SemanticImplementationRole.CommandValidation or
                SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.RulePredicate or SemanticImplementationRole.ReducerTransition)
            {
                throw new InvalidSemanticContract($"Typed context source for requirement '{requirement.RequirementId}' is unresolved.");
            }
            if (matches.GroupBy(value => value.OperationId).Any(group => group.Count() != 1))
            {
                throw new InvalidSemanticContract($"Typed context source for requirement '{requirement.RequirementId}' is ambiguous.");
            }
            result.AddRange(matches);
        }
        return result.ToImmutable();
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticModule module) => module.Features.SelectMany(AllSlices);
    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static bool References(SemanticAuthorization? authorization, HashSet<string> names) => authorization switch
    {
        SemanticPolicyReference reference => names.Contains(reference.Name),
        SemanticLogicalAuthorization logical => References(logical.Left, names) || References(logical.Right, names),
        _ => false
    };

    static SemanticTypedContextDescriptor Policy(SemanticImplementationRequirement requirement, SemanticId operation, IEnumerable<SemanticProperty> properties, SemanticContextSource subject) =>
        Descriptor(
            requirement,
            operation,
            Shaped("Artifact", operation, "authorized-operation", properties),
            new("Subject", Runtime("string"), false, false, subject),
            Fixed("Identity", "Identity"),
            Fixed("Tenant", "TenantId"),
            Fixed("Occurred", "DateTimeOffset"));

    static SemanticTypedContextDescriptor Rule(SemanticImplementationRequirement requirement, SemanticTypedContextMember artifact, SemanticTypedContextMember value, string path) =>
        Descriptor(
            requirement,
            requirement.Source.SemanticId,
            artifact,
            value,
            new("Property", Runtime("string"), false, false, new("context-contract", null, path)),
            Fixed("Tenant", "TenantId"),
            Fixed("CausedBy", "CausedBy"),
            Fixed("Occurred", "DateTimeOffset"),
            Derived("IsWholeArtifact", "bool", "Property"));
}
