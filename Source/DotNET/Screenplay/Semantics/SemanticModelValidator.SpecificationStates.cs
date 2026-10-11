// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateSpecificationEvent(SemanticSpecificationEvent value, bool then = false)
        {
            if (value.EventSource is not null && _semanticVersion == SemanticVersion.V1)
            {
                throw new InvalidSemanticContract("A specification event source requires ESM v2.");
            }

            if (!_events.TryGetValue(value.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Specification event '{value.EventContract}' is unresolved.");
            }

            ValidatePropertyValues(value.Values, eventContract.Properties, true);
            ValidateFixtureRoute(value.Route, value.Unrouted, then, value.EventSource, value.EventContract);
            if (value.EventSource is not null)
            {
                ValidateEventSource(value.EventSource, FixtureEventSourceType(value.Route, value.EventContract));
            }
        }

        void ValidateEventSource(SemanticEventSourceIdentity source, SemanticTypeReference? requiredType)
        {
            if (source.Type.IsCollection || source.Type.IsOptional || (requiredType is not null && source.Type != requiredType))
            {
                throw new InvalidSemanticContract("A specification event source must have the required scalar destination type.");
            }

            ValidateValue(source.Value, source.Type, "specification event source");
        }

        void ValidateSpecificationReadModel(SemanticSpecificationReadModel state, bool requireExact, bool requireIdentifier)
        {
            if (!_readModels.TryGetValue(state.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Specification read model '{state.ReadModel}' is unresolved.");
            }

            var identifier = IdentifierProperty(readModel);
            ValidateValue(state.Key, identifier.Type, "specification read model key");
            ValidatePropertyValues(state.Values, readModel.Properties, requireExact);
            var identifierValue = state.Values.SingleOrDefault(_ => _.TargetProperty == identifier.Id)?.Value;
            if (IsInconsistentIdentifier(state.Key, identifierValue, requireIdentifier))
            {
                throw new InvalidSemanticContract("A specification read model key must equal its identifier property value.");
            }
        }

        bool IsInconsistentIdentifier(SemanticValue key, SemanticValue? identifierValue, bool requireIdentifier) =>
            identifierValue is null ? requireIdentifier : !SemanticValueRules.AreEqual(key, identifierValue);

        void ValidateSpecificationQuery(SemanticSpecificationQueryResult result)
        {
            if (!_queries.TryGetValue(result.Query, out var query))
            {
                throw new InvalidSemanticContract($"Specification query '{result.Query}' is unresolved.");
            }

            if (query.Argument is not null) ValidateValue(result.Key, query.Argument.Type, "specification query key");
            RequireObjects(result.Results, nameof(result.Results), "specification query result state");
            if (!IsValidResultCount(query.Cardinality, result.Results.Length))
            {
                throw new InvalidSemanticContract("A specification query result count does not match its cardinality.");
            }

            var keyProperty = query.KeyProperty is { } queryKeyProperty
                ? _readModels[query.ReadModel].Properties.Single(_ => _.Id == queryKeyProperty)
                : null;
            foreach (var state in result.Results)
            {
                ValidateQueryResultState(query, keyProperty, result, state);
            }

            RejectDuplicateReadModelStates(result.Results, "query result read model");
        }

        bool IsValidResultCount(SemanticQueryCardinality cardinality, int count) => cardinality switch
        {
            SemanticQueryCardinality.One => count == 1,
            SemanticQueryCardinality.ZeroOrOne => count <= 1,
            SemanticQueryCardinality.Many => true,
            _ => false
        };

        void ValidateQueryResultState(
            SemanticKeyedQuery query,
            SemanticProperty? keyProperty,
            SemanticSpecificationQueryResult result,
            SemanticSpecificationReadModel state)
        {
            if (state.ReadModel != query.ReadModel)
            {
                throw new InvalidSemanticContract("A specification query result uses the wrong read model or key.");
            }

            ValidateSpecificationReadModel(state, false, false);
            if (keyProperty is null)
            {
                return;
            }

            var queryKeyValue = state.Values.SingleOrDefault(_ => _.TargetProperty == keyProperty.Id)?.Value;
            if (queryKeyValue is null && keyProperty.IsIdentifier)
            {
                queryKeyValue = state.Key;
            }

            if (!MatchesQueryKey(state, keyProperty, queryKeyValue, result.Key))
            {
                throw new InvalidSemanticContract("A specification query result uses the wrong read model or key.");
            }
        }

        bool MatchesQueryKey(SemanticSpecificationReadModel state, SemanticProperty keyProperty, SemanticValue? queryKeyValue, SemanticValue resultKey) =>
            queryKeyValue is not null && SemanticValueRules.AreEqual(queryKeyValue, resultKey) &&
            (!keyProperty.IsIdentifier || SemanticValueRules.AreEqual(state.Key, resultKey));

        void ValidateSpecificationError(SemanticSpecificationError error)
        {
            if (!IsAdmittedRejection(error))
            {
                throw new InvalidSemanticContract("A specification rejection must be bare, message-only, or contain both a non-empty code and message.");
            }
        }

        bool IsAdmittedRejection(SemanticSpecificationError error) => error.Code is null
            ? error.Message is null || error.Message.Length > 0
            : error.Code.Length > 0 && !string.IsNullOrEmpty(error.Message);

        void ValidatePropertyValues(
            ImmutableArray<SemanticPropertyValue> values,
            ImmutableArray<SemanticProperty> targetProperties,
            bool requireExact,
            HashSet<SemanticId>? routedInputs = null)
        {
            RequireObjects(values, nameof(values), "property value");
            var targets = Properties(targetProperties);
            var assigned = new HashSet<SemanticId>();
            foreach (var value in values)
            {
                RejectNull(value.Value, "semantic value");
                if (!assigned.Add(value.TargetProperty))
                {
                    throw new InvalidSemanticContract($"Property value target '{value.TargetProperty}' is duplicated.");
                }

                if (!targets.TryGetValue(value.TargetProperty, out var target))
                {
                    throw new InvalidSemanticContract($"Property value target '{value.TargetProperty}' is unresolved.");
                }

                ValidateValue(value.Value, target.Type, "property value", routedInputs?.Contains(target.Id) == true);
            }

            if (requireExact && assigned.Count != targets.Count)
            {
                throw new InvalidSemanticContract("A specification property value shape must assign every target exactly once.");
            }
        }

        void RejectDuplicateReadModelStates(
            ImmutableArray<SemanticSpecificationReadModel> states,
            string description) =>
            RejectDuplicateKeys(states, state => state.ReadModel, state => state.Key, $"A specification contains a duplicated {description} state key.");

        void RejectDuplicateAbsentReadModels(ImmutableArray<SemanticSpecificationAbsentReadModel> states) =>
            RejectDuplicateKeys(states, state => state.ReadModel, state => state.Key, "A specification contains a duplicated absent read model key.");

        void RejectDuplicateQueryExpectations(ImmutableArray<SemanticSpecificationQueryResult> results) =>
            RejectDuplicateKeys(results, result => result.Query, result => result.Key, "A specification contains a duplicated query expectation key.");

        void RejectDuplicateKeys<T>(ImmutableArray<T> items, Func<T, SemanticId> owner, Func<T, SemanticValue> key, string message)
        {
            for (var first = 0; first < items.Length; first++)
            {
                for (var second = first + 1; second < items.Length; second++)
                {
                    if (owner(items[first]) == owner(items[second]) &&
                        SemanticValueRules.AreEqual(key(items[first]), key(items[second])))
                    {
                        throw new InvalidSemanticContract(message);
                    }
                }
            }
        }

        void RequireSpecificationArrays(SemanticSpecification specification)
        {
            RequireObjects(specification.GivenEvents, nameof(specification.GivenEvents), "specification event");
            RequireObjects(specification.GivenReadModels, nameof(specification.GivenReadModels), "specification read model");
            if (specification.When is not null)
            {
                RequireObjects(specification.When.Values, nameof(specification.When.Values), "property value");
            }
            if (specification.WhenAppended is not null) RequireObjects(specification.WhenAppended.Values, nameof(specification.WhenAppended.Values), "property value");
            RequireObjects(specification.ThenEvents, nameof(specification.ThenEvents), "specification event");
            RequireObjects(specification.ThenReadModels, nameof(specification.ThenReadModels), "specification read model");
            RequireObjects(specification.ThenAbsentReadModels, nameof(specification.ThenAbsentReadModels), "specification absent read model");
            RequireObjects(specification.ThenQueries, nameof(specification.ThenQueries), "specification query result");
            RequireObjects(specification.ThenErrors, nameof(specification.ThenErrors), "specification error");
        }
    }
}
