// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        readonly Dictionary<string, SemanticReadModel> _validatedDecisionReads = new(StringComparer.Ordinal);
        bool _allowDecisionReadPaths;

        void ValidateDecisionReads(SemanticCommand command)
        {
            _validatedDecisionReads.Clear();
            RequireObjects(command.Reads, nameof(command.Reads), "decision read");
            if (command.Reads.IsEmpty) return;
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V25) || _eventRoutesApplication.CommandReadMode != SemanticCommandReadMode.Protected)
                throw new InvalidSemanticContract("Command reads require ESM v25 and protected command read mode.");
            RejectDuplicateNames(command.Reads.Select(read => read.Name), "decision read");
            var slices = AllSlices(_eventRoutesApplication).ToArray();
            foreach (var read in command.Reads)
            {
                if (read.Presence != SemanticDecisionReadPresence.Required || !_readModels.TryGetValue(read.ReadModel, out var view))
                    throw new InvalidSemanticContract("A decision read requires one known read model and required presence.");
                var key = command.Properties.SingleOrDefault(property => property.Id == read.Key)
                    ?? throw new InvalidSemanticContract("A decision read key must be a declared command input property.");
                if (SemanticDecisionReadAdmission.Key(key, _concepts) is { } keyRefusal)
                    throw new InvalidSemanticContract($"Decision read '{read.Name}' is unprotectable: {keyRefusal}.");
                var identifiers = view.Properties.Where(property => property.IsIdentifier).ToArray();
                if (identifiers.Length != 1) throw new InvalidSemanticContract("A decision read view must have one event-source instance key.");
                RequireCompatible(key.Type, identifiers[0].Type, "decision read key");
                var projections = slices.SelectMany(slice => slice.Projections).Where(projection => projection.Target == SemanticProjectionTargetKind.ReadModel && projection.ReadModel == view.Id).ToArray();
                var routed = SemanticDecisionReadAdmission.Routed(command.Route is not null, SemanticDecisionReadAdmission.HasPossibleEventRoute(command), projections, SemanticDecisionReadAdmission.Producers(slices));
                var reducer = slices.SelectMany(slice => slice.Reducers).Any(value => value.ReadModel == view.Id);
                if (SemanticDecisionReadAdmission.View(projections, reducer, routed) is { } refusal)
                    throw new InvalidSemanticContract($"Decision read '{read.Name}' is unprotectable: {refusal}.");
                if (command.Reads.Count(other => other.ReadModel == read.ReadModel) > 1 && read.Name == view.Name)
                    throw new InvalidSemanticContract("Repeated reads of a view require distinguishing aliases on every read.");
                if (command.Properties.Any(property => property.Name == read.Name))
                    throw new InvalidSemanticContract("A read alias cannot collide with a command property.");
                _validatedDecisionReads.Add(read.Name, view);
            }
        }

        SemanticTypeReference ResolveDecisionReadProperty(string read, SemanticId property)
        {
            if (!_allowDecisionReadPaths || !_semanticVersion.IsAtLeast(SemanticVersion.V25) ||
                !_validatedDecisionReads.TryGetValue(read, out var view) || view.Properties.SingleOrDefault(value => value.Id == property) is not { } member)
            {
                throw new InvalidSemanticContract("A read path requires ESM v25, a declared command read and a view property, and is only admitted in requirements and produced-event mappings.");
            }
            return member.Type;
        }

        void WithDecisionReadPaths(Action validate)
        {
            var previous = _allowDecisionReadPaths;
            _allowDecisionReadPaths = true;
            try { validate(); }
            finally { _allowDecisionReadPaths = previous; }
        }
    }
}
