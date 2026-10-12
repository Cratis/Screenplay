// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void RegisterConcept(SemanticConcept concept)
        {
            Register(concept.Id, concept.Name, "concept");
            RequireArray(concept.Values, nameof(concept.Values));
            RequireObjects(concept.Validations, nameof(concept.Validations), "validation rule");
            RejectDuplicateNames(concept.Values, $"value on concept '{concept.Name}'");
            _concepts.Add(concept.Id, concept);
        }

        void RegisterType(SemanticCompositeType type)
        {
            Register(type.Id, type.Name, "composite type");
            RegisterProperties(type.Properties, $"composite type '{type.Name}'");
            _types.Add(type.Id, type);
        }

        void RegisterModule(SemanticModule module)
        {
            Register(module.Id, module.Name, "module");
            RequireObjects(module.Features, nameof(module.Features), "feature");
            RejectDuplicateNames(module.Features.Select(_ => _.Name), $"feature in module '{module.Name}'");
            foreach (var feature in module.Features)
            {
                RegisterFeature(feature);
            }
        }

        void RegisterFeature(SemanticFeature feature)
        {
            Register(feature.Id, feature.Name, "feature");
            RequireObjects(feature.Features, nameof(feature.Features), "nested feature");
            RequireObjects(feature.Slices, nameof(feature.Slices), "slice");
            RejectDuplicateNames(feature.Features.Select(_ => _.Name), $"nested feature in '{feature.Name}'");
            RejectDuplicateNames(feature.Slices.Select(_ => _.Name), $"slice in feature '{feature.Name}'");
            foreach (var nested in feature.Features)
            {
                RegisterFeature(nested);
            }

            foreach (var slice in feature.Slices)
            {
                RegisterSlice(slice);
            }
        }

        void RegisterSlice(SemanticSlice slice)
        {
            Register(slice.Id, slice.Name, "slice");
            RequireObjects(slice.Events, nameof(slice.Events), "event contract");
            RequireObjects(slice.Commands, nameof(slice.Commands), "command");
            RequireObjects(slice.ReadModels, nameof(slice.ReadModels), "read model");
            RequireObjects(slice.Projections, nameof(slice.Projections), "projection");
            RequireObjects(slice.Reducers, nameof(slice.Reducers), "reducer");
            RejectDuplicateNames(slice.Reducers.Select(_ => _.Name), $"reducer in slice '{slice.Name}'");
            RequireObjects(slice.Queries, nameof(slice.Queries), "query");
            RequireObjects(slice.Specifications, nameof(slice.Specifications), "specification");
            RejectDuplicateNames(slice.Events.Select(_ => _.Name), $"event in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Commands.Select(_ => _.Name), $"command in slice '{slice.Name}'");
            RejectDuplicateNames(slice.ReadModels.Select(_ => _.Name), $"read model in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Projections.Select(_ => _.Name), $"projection in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Queries.Select(_ => _.Name), $"query in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Specifications.Select(_ => _.Name), $"specification in slice '{slice.Name}'");

            foreach (var eventContract in slice.Events)
            {
                RegisterEvent(eventContract);
            }

            foreach (var command in slice.Commands)
            {
                RegisterCommand(command);
            }

            foreach (var readModel in slice.ReadModels)
            {
                RegisterReadModel(readModel);
            }

            foreach (var projection in slice.Projections)
            {
                Register(projection.Id, projection.Name, "projection");
                RequireObjects(projection.Transitions, nameof(projection.Transitions), "projection transition");
            }

            foreach (var query in slice.Queries)
            {
                Register(query.Id, query.Name, "query");
                if (query.Argument is not null)
                {
                    Register(query.Argument.Id, query.Argument.Name, "query argument");
                }

                RequireObjects(query.Filters, nameof(query.Filters), "query filter");
                foreach (var filter in query.Filters) Register(filter.Id, filter.Name, "query filter");
                _queries.Add(query.Id, query);
            }

            foreach (var specification in slice.Specifications)
            {
                Register(specification.Id, specification.Name, "specification");
                RequireSpecificationArrays(specification);
            }

            RegisterConstraints(slice);
            RegisterAutomation(slice);
        }

        void RegisterEvent(SemanticEventContract eventContract)
        {
            Register(eventContract.Id, eventContract.Name, "event contract");
            RequireValidContractRevision(eventContract);
            RequireCompleteLineage(eventContract);
            RegisterPriorRevisions(eventContract);
            if (!_eventContractIds.Add(eventContract.ContractId))
            {
                throw new InvalidSemanticContract($"Event contract identity '{eventContract.ContractId}' is duplicated.");
            }

            ValidateTags(eventContract.Tags);
            _events.Add(eventContract.Id, eventContract);
            RegisterProperties(eventContract.Properties, $"event contract '{eventContract.Name}'");
        }

        void RequireValidContractRevision(SemanticEventContract eventContract)
        {
            if (!eventContract.ContractId.IsSet || !eventContract.Revision.IsValid || eventContract.Revision.Value == uint.MaxValue)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has an invalid contract revision.");
            }
        }

        void RequireCompleteLineage(SemanticEventContract eventContract)
        {
            EventContractRevision? expectedPredecessor = eventContract.Revision == EventContractRevision.Initial ? null : new EventContractRevision(eventContract.Revision.Value - 1);
            if (eventContract.PriorRevisions.IsDefault || eventContract.PriorRevisions.Length != eventContract.Revision.Value - 1 ||
                eventContract.Predecessor != expectedPredecessor)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has incomplete predecessor lineage.");
            }
        }

        void RegisterPriorRevisions(SemanticEventContract eventContract)
        {
            for (var index = 0; index < eventContract.PriorRevisions.Length; index++)
            {
                var prior = eventContract.PriorRevisions[index];
                if (!IsValidPriorRevision(prior, index))
                {
                    throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has invalid historical revision lineage.");
                }

                ValidateTags(prior.Tags);
                RegisterProperties(prior.Properties, $"event contract '{eventContract.Name}' revision {prior.Revision.Value}");
            }
        }

        bool IsValidPriorRevision(SemanticEventRevision? prior, int index) =>
            prior is not null && prior.Revision.Value == (uint)index + 1 &&
            prior.Predecessor == (index == 0 ? null : new EventContractRevision((uint)index)) && !prior.Properties.IsDefault;

        void RegisterCommand(SemanticCommand command)
        {
            Register(command.Id, command.Name, "command");
            RegisterProperties(command.Properties, $"command '{command.Name}'");
            RequireObjects(command.Validations, nameof(command.Validations), "validation rule");
            RequireObjects(command.Produces, nameof(command.Produces), "produced event");
            _commands.Add(command.Id, command);
        }

        void RegisterReadModel(SemanticReadModel readModel)
        {
            Register(readModel.Id, readModel.Name, "read model");
            RegisterProperties(readModel.Properties, $"read model '{readModel.Name}'");
            _readModels.Add(readModel.Id, readModel);
        }

        void RegisterProperties(ImmutableArray<SemanticProperty> properties, string owner)
        {
            RequireObjects(properties, nameof(properties), "property");
            RejectDuplicateNames(properties.Select(_ => _.Name), $"property on {owner}");
            foreach (var property in properties)
            {
                Register(property.Id, property.Name, "property");
            }
        }
    }
}
