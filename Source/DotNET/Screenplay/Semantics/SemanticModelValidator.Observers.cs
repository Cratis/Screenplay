// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateProjection(SemanticProjection projection)
        {
            if (projection.Target == SemanticProjectionTargetKind.Event)
            {
                ValidateEventTargetProjection(projection);
                return;
            }

            if (projection.Target != SemanticProjectionTargetKind.ReadModel)
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' has an unknown target kind.");
            }

            if (!_readModels.TryGetValue(projection.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Projection read model '{projection.ReadModel}' is unresolved.");
            }

            if (projection.Scope is not null)
            {
                ValidateProjectionScope(projection, readModel);
                return;
            }

            var targets = Properties(readModel.Properties);
            var identifier = IdentifierProperty(readModel);
            foreach (var transition in projection.Transitions)
            {
                ValidateReadModelTransition(transition, targets, identifier);
            }
        }

        void ValidateReadModelTransition(SemanticProjectionTransition transition, Dictionary<SemanticId, SemanticProperty> targets, SemanticProperty identifier)
        {
            if (!_events.TryGetValue(transition.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Projection event contract '{transition.EventContract}' is unresolved.");
            }

            RejectNull(transition.AffectedInstance, "affected instance");
            ValidateEnum(transition.AffectedInstance.Cardinality, AffectedInstanceCardinality.Unknown, "affected instance cardinality");
            if (_semanticVersion.IsAtLeast(SemanticVersion.V4) && transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One)
            {
                throw new InvalidSemanticContract("ESM v4 does not admit zeroOrOne or many projection transition cardinality.");
            }

            var sources = Properties(eventContract.Properties);
            var keyType = ResolveExpression(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, sources) ??
                throw new InvalidSemanticContract("An affected-instance key cannot be null.");
            RequireKeyMatchesCardinality(keyType, transition.AffectedInstance.Cardinality, identifier);
            ValidateMappings(transition.Mappings, targets, SemanticExpressionRootKind.Event, sources);
        }

        void RequireKeyMatchesCardinality(SemanticTypeReference keyType, AffectedInstanceCardinality cardinality, SemanticProperty identifier)
        {
            if (!HasKeyShape(keyType, cardinality) || !SameValueType(keyType, identifier.Type))
            {
                throw new InvalidSemanticContract("An affected-instance key type, optionality or cardinality is incompatible with the read model identifier.");
            }
        }

        bool HasKeyShape(SemanticTypeReference keyType, AffectedInstanceCardinality cardinality) =>
            keyType.IsCollection == (cardinality == AffectedInstanceCardinality.Many) &&
            keyType.IsOptional == (cardinality == AffectedInstanceCardinality.ZeroOrOne);

        void ValidateReducer(SemanticReducer reducer)
        {
            ValidateObserverFilter(reducer.From);
            var targetsEvent = reducer.Target == SemanticProjectionTargetKind.Event;
            if (reducer.Target is not (SemanticProjectionTargetKind.ReadModel or SemanticProjectionTargetKind.Event))
            {
                throw new InvalidSemanticContract($"Reducer '{reducer.Name}' has an unknown target kind.");
            }

            if (targetsEvent)
            {
                RequirePublicEventTarget(reducer.ReadModel, $"Reducer '{reducer.Name}'");
            }

            if (!IsCompleteReducer(reducer, targetsEvent))
            {
                throw new InvalidSemanticContract($"Reducer '{reducer.Name}' requires ESM v3, a read model, and transitions.");
            }

            var events = new HashSet<SemanticId>();
            var requirements = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transition in reducer.Transitions)
            {
                if (!TryAcceptReducerTransition(transition, events, requirements))
                {
                    throw new InvalidSemanticContract($"Reducer '{reducer.Name}' has an unresolved or duplicate transition.");
                }
            }
        }

        bool IsCompleteReducer(SemanticReducer reducer, bool targetsEvent) =>
            _semanticVersion.IsAtLeast(SemanticVersion.V3) && !string.IsNullOrWhiteSpace(reducer.Name) &&
            (targetsEvent || _readModels.ContainsKey(reducer.ReadModel)) && !reducer.Transitions.IsDefaultOrEmpty;

        bool TryAcceptReducerTransition(SemanticReducerTransition? transition, HashSet<SemanticId> events, HashSet<string> requirements) =>
            transition is not null && _events.ContainsKey(transition.EventContract) &&
            !string.IsNullOrWhiteSpace(transition.RequirementId) &&
            events.Add(transition.EventContract) && requirements.Add(transition.RequirementId);

        void ValidateQuery(SemanticKeyedQuery query)
        {
            if (!_readModels.TryGetValue(query.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Query read model '{query.ReadModel}' is unresolved.");
            }

            ValidateEnum(query.Cardinality, SemanticQueryCardinality.Unknown, "query cardinality");
            ValidateEnum(query.Delivery, SemanticQueryDelivery.Unknown, "query delivery");
            if ((query.Argument is null) != (query.KeyProperty is null))
            {
                throw new InvalidSemanticContract("A keyed query must declare both an argument and a key property; an unkeyed query declares neither.");
            }

            ValidateQueryFilters(query, readModel);
            if (query.Argument is null)
            {
                RequireCollectionResult(query);
                return;
            }

            ValidateQueryKey(query, query.Argument, readModel);
        }

        void RequireCollectionResult(SemanticKeyedQuery query)
        {
            if (query.Cardinality != SemanticQueryCardinality.Many)
            {
                throw new InvalidSemanticContract("An unkeyed query must return a collection.");
            }
        }

        bool IsSameScalar(SemanticTypeReference argument, SemanticTypeReference key) =>
            !argument.IsCollection && !key.IsCollection && SameType(argument, key);

        void ValidateQueryKey(SemanticKeyedQuery query, SemanticReadModelQueryArgument argument, SemanticReadModel readModel)
        {
            ValidateTypeReference(argument.Type);
            if (!Properties(readModel.Properties).TryGetValue(query.KeyProperty!.Value, out var keyProperty))
            {
                throw new InvalidSemanticContract($"Query key property '{query.KeyProperty}' is unresolved.");
            }

            if (!IsSameScalar(argument.Type, keyProperty.Type))
            {
                throw new InvalidSemanticContract("A query argument and key property must have the same scalar type and optionality.");
            }

            if (query.Cardinality == SemanticQueryCardinality.ZeroOrOne && !keyProperty.IsIdentifier)
            {
                throw new InvalidSemanticContract("An optional lookup query key property must identify one read-model instance.");
            }
        }
    }
}
