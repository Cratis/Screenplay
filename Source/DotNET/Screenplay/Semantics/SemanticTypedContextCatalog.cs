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

    static SemanticContextType Runtime(string token) => new(SemanticContextTypeKinds.Runtime, null, null, token);
    static SemanticContextType Shape(SemanticId id) => new(SemanticContextTypeKinds.Shape, null, id, null);
    static SemanticContextType Model(SemanticTypeReference type)
    {
        if (type.Kind == SemanticTypeReferenceKind.Unknown ||
            (type.Kind == SemanticTypeReferenceKind.Primitive && type.Primitive == SemanticPrimitiveType.Unknown) ||
            (type.Kind is SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType && type.Target == default))
        {
            throw new InvalidSemanticContract("Typed context has an unresolved model type.");
        }

        return new(SemanticContextTypeKinds.Model, type, null, null);
    }

    static SemanticTypedContextMember Fixed(string name, string token) =>
        new(name, Runtime(token), false, false, new(SemanticContextSourceKinds.ContextContract, null, name));

    static SemanticTypedContextMember Derived(string name, string token, string from) =>
        new(name, Runtime(token), false, true, new(SemanticContextSourceKinds.Derived, null, from));

    static SemanticTypedContextMember Shaped(string name, SemanticId id, string kind, IEnumerable<SemanticProperty> properties, bool nullable = false) =>
        new(name, Shape(id) with { Properties = [.. properties.Select(value => new SemanticContextProperty(value.Name, value.Id, Model(value.Type).ModelType!))] }, nullable, false, new(kind, id, name));

    static SemanticTypedContextMember Typed(string name, SemanticTypeReference type, SemanticId id, string path) =>
        new(name, Model(type), type.IsOptional, false, new(SemanticContextSourceKinds.ModelProperty, id, path));

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

        // A failed bind never publishes bound-role descriptors, even if some declarations were resolved.
        if (!strict) requirements = [.. requirements.Where(value => value.Role == SemanticImplementationRole.CommandHandler)];
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
                            Shaped("Command", command.Id, SemanticContextSourceKinds.Command, command.Properties),
                            Fixed("Tenant", SemanticContextRuntimeTokens.TenantId),
                            Fixed("Identity", SemanticContextRuntimeTokens.Identity),
                            Fixed("CausedBy", SemanticContextRuntimeTokens.CausedBy),
                            Fixed("Causation", SemanticContextRuntimeTokens.Causation),
                            Fixed("Occurred", SemanticContextRuntimeTokens.DateTime)));
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
                            matches.Add(Rule(requirement, Shaped("Artifact", command.Id, SemanticContextSourceKinds.Command, command.Properties), Shaped("Value", command.Id, SemanticContextSourceKinds.Command, command.Properties), null, string.Empty));
                        }
                        foreach (var rule in rules)
                        {
                            var property = command.Properties.SingleOrDefault(value => value.Id == rule.Property);
                            if (property is not null)
                            {
                                matches.Add(Rule(requirement, Shaped("Artifact", command.Id, SemanticContextSourceKinds.Command, command.Properties), Typed("Value", property.Type, property.Id, property.Name), property.Id, property.Name));
                            }
                        }
                    }
                    foreach (var concept in application.Concepts.Where(value => value.Id == requirement.Source.SemanticId))
                    {
                        if (concept.Validations.Any(value => value.RequirementId == requirement.RequirementId))
                        {
                            var type = Model(SemanticTypeReference.ForConcept(concept.Id));
                            matches.Add(Rule(
                                requirement,
                                new("Artifact", type, false, false, new(SemanticContextSourceKinds.ConceptValue, concept.Id, "value")),
                                new("Value", type, false, false, new(SemanticContextSourceKinds.ConceptValue, concept.Id, "value")),
                                null,
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
                                Shaped("State", state.Id, SemanticContextSourceKinds.ReadModel, state.Properties, true),
                                Shaped("Event", @event.Id, SemanticContextSourceKinds.CurrentEvent, @event.Properties) with { Source = new(SemanticContextSourceKinds.CurrentEvent, @event.Id, "Event") { EventRevision = @event.Revision } },
                                new("Key", Runtime(SemanticContextRuntimeTokens.Text), false, false, new(SemanticContextSourceKinds.EventSourceId, @event.Id, "eventSourceId")),
                                Fixed("Tenant", SemanticContextRuntimeTokens.TenantId),
                                Fixed("Occurred", SemanticContextRuntimeTokens.DateTime),
                                Fixed("SequenceNumber", SemanticContextRuntimeTokens.WholeNumber),
                                Derived("IsFirst", SemanticContextRuntimeTokens.Boolean, "State")));
                        }
                    }
                    break;
                case SemanticImplementationRole.PolicyPredicate:
                    // Policy references in the ESM are name-only; use-site joins necessarily use the declared name.
                    var policyNames = application.Policies.Where(value => value.Condition is SemanticOpaquePolicyCondition opaque && opaque.RequirementId == requirement.RequirementId)
                        .Select(value => value.Name).ToHashSet(StringComparer.Ordinal);
                    foreach (var command in commands.Where(value => References(value.Authorization, policyNames)))
                    {
                        var identifier = command.Properties.FirstOrDefault(value => value.IsIdentifier);
                        var subject = identifier is null
                            ? new SemanticContextSource(SemanticContextSourceKinds.Unavailable, null, string.Empty)
                            : new SemanticContextSource(SemanticContextSourceKinds.CommandIdentifier, identifier.Id, identifier.Name);
                        matches.Add(Policy(requirement, command.Id, command.Properties, subject));
                    }
                    foreach (var query in queries.Where(value => References(value.Authorization, policyNames)))
                    {
                        matches.Add(Policy(
                            requirement,
                            query.Id,
                            [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)],
                            new SemanticContextSource(SemanticContextSourceKinds.QueryKey, query.Argument.Id, query.Argument.Name)));
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
            foreach (var descriptor in matches)
            {
                try
                {
                    result.Add(descriptor with { Types = TypeClosure(application, descriptor), HasResolvedTypes = true });
                }
                catch (InvalidSemanticContract) when (!strict)
                {
                    // No ESM exists to resolve a missing definition; this descriptor is not renderable.
                    result.Add(descriptor);
                }
            }
        }
        return result.ToImmutable();
    }

    static ImmutableArray<SemanticContextTypeDefinition> TypeClosure(SemanticApplication application, SemanticTypedContextDescriptor descriptor)
    {
        var definitions = ImmutableArray.CreateBuilder<SemanticContextTypeDefinition>();
        var visited = new HashSet<SemanticId>();
        void Visit(SemanticTypeReference type)
        {
            if (type.Kind == SemanticTypeReferenceKind.Primitive) return;
            if (type.Kind is not (SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType) || type.Target == default)
                throw new InvalidSemanticContract("Typed context has an unresolved model type.");
            if (!visited.Add(type.Target)) return;
            if (type.Kind == SemanticTypeReferenceKind.Concept)
            {
                var concept = application.Concepts.SingleOrDefault(value => value.Id == type.Target)
                    ?? throw new InvalidSemanticContract($"Typed context concept '{type.Target}' is unresolved.");
                definitions.Add(new(concept.Id, concept.Name, type.Kind, concept.Primitive, []));
            }
            else
            {
                var composite = application.Types.SingleOrDefault(value => value.Id == type.Target)
                    ?? throw new InvalidSemanticContract($"Typed context composite type '{type.Target}' is unresolved.");
                var properties = composite.Properties.Select(value => new SemanticContextProperty(value.Name, value.Id, Model(value.Type).ModelType!)).ToImmutableArray();
                definitions.Add(new(composite.Id, composite.Name, type.Kind, SemanticPrimitiveType.Unknown, properties));
                foreach (var property in properties) Visit(property.Type);
            }
        }
        foreach (var member in descriptor.Members)
        {
            if (member.Type.ModelType is { } type) Visit(type);
            foreach (var property in member.Type.Properties) Visit(property.Type);
        }
        return definitions.ToImmutable();
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
            Shaped("Artifact", operation, SemanticContextSourceKinds.AuthorizedOperation, properties),
            new("Subject", Runtime(SemanticContextRuntimeTokens.Text), false, false, subject),
            Fixed("Identity", SemanticContextRuntimeTokens.Identity),
            Fixed("Tenant", SemanticContextRuntimeTokens.TenantId),
            Fixed("Occurred", SemanticContextRuntimeTokens.DateTime));

    static SemanticTypedContextDescriptor Rule(SemanticImplementationRequirement requirement, SemanticTypedContextMember artifact, SemanticTypedContextMember value, SemanticId? propertyId, string path) =>
        Descriptor(
            requirement,
            requirement.Source.SemanticId,
            artifact,
            value,
            new("Property", Runtime(SemanticContextRuntimeTokens.Text), false, false, new(propertyId is null ? SemanticContextSourceKinds.ValidatedArtifact : SemanticContextSourceKinds.ValidatedProperty, propertyId ?? requirement.Source.SemanticId, string.Empty) { ConstantValue = path }),
            Fixed("Tenant", SemanticContextRuntimeTokens.TenantId),
            Fixed("CausedBy", SemanticContextRuntimeTokens.CausedBy),
            Fixed("Occurred", SemanticContextRuntimeTokens.DateTime),
            Derived("IsWholeArtifact", SemanticContextRuntimeTokens.Boolean, "Property"));
}
