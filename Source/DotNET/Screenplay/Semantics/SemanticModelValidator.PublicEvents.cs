// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static bool UsesPublicEvents(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).Any(slice =>
            slice.Direction is not null ||
            slice.Events.Any(@event => @event.Visibility != SemanticEventVisibility.Private || @event.Origin is not null) ||
            slice.Projections.Any(projection => projection.Target != SemanticProjectionTargetKind.ReadModel) ||
            slice.Reducers.Any(reducer => reducer.Target != SemanticProjectionTargetKind.ReadModel) ||
            slice.Captures.Any(capture => capture.EventsSource is not null));

    static void ValidatePublicEventsVersion(SemanticApplication application, SemanticVersion version)
    {
        var uses = UsesPublicEvents(application);
        if (version == SemanticVersion.V9 && !uses && !SemanticEventRouting.Uses(application))
        {
            throw new InvalidSemanticContract("A public events model must declare a public event, an event origin, a translation direction, an event-target projection or reducer, or an events-source capture.");
        }

        if (!version.IsAtLeast(SemanticVersion.V9) && uses)
        {
            throw new InvalidSemanticContract("Public events, translation direction, event-target projections and reducers, and events-source captures require ESM v9.");
        }
    }

    private sealed partial class ValidationContext
    {
        static IEnumerable<SemanticId> ObservedEvents(SemanticProjection projection)
        {
            if (projection.Scope is null)
            {
                return projection.Transitions.Select(transition => transition.EventContract);
            }

            return projection.Scope.From.Select(from => from.EventContract).Concat(projection.Scope.Joins.Select(join => join.EventContract));
        }

        // Slice-level rules of the public event contract, held on the semantic model so that a programmatically built or
        // deserialized model fails closed the way a bound one does.
        void ValidatePublicEventSlice(SemanticSlice slice)
        {
            foreach (var @event in slice.Events)
            {
                if (@event.Visibility is not (SemanticEventVisibility.Private or SemanticEventVisibility.Public))
                {
                    throw new InvalidSemanticContract($"Event '{@event.Name}' has an unknown visibility.");
                }

                if (@event.Origin is not null && (string.IsNullOrWhiteSpace(@event.Origin) || @event.Visibility != SemanticEventVisibility.Public))
                {
                    throw new InvalidSemanticContract($"Event '{@event.Name}' origin must be a nonblank store name on a public event.");
                }
            }

            if (slice.Direction is not null)
            {
                if (slice.Kind != SemanticSliceKind.Translate || slice.Direction is not (SemanticTranslationDirection.Inbound or SemanticTranslationDirection.Outbound))
                {
                    throw new InvalidSemanticContract($"Slice '{slice.Name}' declares a direction, which only a Translate slice may.");
                }
            }

            foreach (var produced in slice.Commands.SelectMany(command => command.Produces))
            {
                if (_events.TryGetValue(produced.EventContract, out var producedContract) && producedContract.Visibility == SemanticEventVisibility.Public)
                {
                    throw new InvalidSemanticContract($"Command in slice '{slice.Name}' cannot produce public event '{producedContract.Name}'; an outbound translation publishes it.");
                }
            }

            var outbound = slice.Direction == SemanticTranslationDirection.Outbound;
            var localPublic = slice.Events.Where(@event => @event.Visibility == SemanticEventVisibility.Public && @event.Origin is null).ToArray();
            var eventTargets = slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event).Select(projection => projection.ReadModel)
                .Concat(slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event).Select(reducer => reducer.ReadModel))
                .ToArray();
            if (eventTargets.Length > 0 && !outbound)
            {
                throw new InvalidSemanticContract($"Slice '{slice.Name}' targets an event, which only a Translate slice with direction outbound may.");
            }

            if (outbound)
            {
                if (localPublic.Length != 1)
                {
                    throw new InvalidSemanticContract($"Outbound slice '{slice.Name}' must declare exactly one local public event; found {localPublic.Length}.");
                }

                if (eventTargets.Any(target => target != localPublic[0].Id))
                {
                    throw new InvalidSemanticContract($"Outbound slice '{slice.Name}' may target only its own public event.");
                }

                if (slice.Captures.Length > 0)
                {
                    throw new InvalidSemanticContract($"Outbound slice '{slice.Name}' consumes private events and cannot declare a capture.");
                }

                foreach (var consumed in slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event).SelectMany(ObservedEvents)
                    .Concat(slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event).SelectMany(reducer => reducer.Transitions.Select(transition => transition.EventContract))))
                {
                    if (_events.TryGetValue(consumed, out var contract) && contract.Visibility == SemanticEventVisibility.Public)
                    {
                        throw new InvalidSemanticContract($"Outbound slice '{slice.Name}' folds private events; '{contract.Name}' is public.");
                    }
                }
            }

            foreach (var capture in slice.Captures.Where(capture => capture.EventsSource is not null))
            {
                var source = capture.EventsSource!;
                if (slice.Direction != SemanticTranslationDirection.Inbound)
                {
                    throw new InvalidSemanticContract($"Capture '{capture.Name}' reads events, which only a Translate slice with direction inbound may.");
                }

                if (source.Events.IsDefaultOrEmpty || source.Events.Distinct().Count() != source.Events.Length)
                {
                    throw new InvalidSemanticContract($"Capture '{capture.Name}' must read one or more distinct events.");
                }

                foreach (var id in source.Events)
                {
                    if (!_events.TryGetValue(id, out var contract) || contract.Visibility != SemanticEventVisibility.Public || contract.Origin is null)
                    {
                        throw new InvalidSemanticContract($"Capture '{capture.Name}' may read only declared public events with an origin.");
                    }
                }
            }
        }

        void RequirePublicEventTarget(SemanticId id, string owner)
        {
            if (!_events.TryGetValue(id, out var contract) || contract.Visibility != SemanticEventVisibility.Public || contract.Origin is not null)
            {
                throw new InvalidSemanticContract($"{owner} target must be a declared local public event.");
            }
        }

        void ValidateEventTargetProjection(SemanticProjection projection)
        {
            RequirePublicEventTarget(projection.ReadModel, $"Projection '{projection.Name}'");
            if (projection.Scope is null || !projection.Transitions.IsEmpty)
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' targeting an event must carry a scope and no flat transitions.");
            }

            var target = _events[projection.ReadModel];
            var level = new ProjectionLevel(
                Properties(target.Properties),
                SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
                null,
                ProjectionLevelKind.Root,
                string.Empty);
            ValidateScope(projection.Scope, level);
            if (projection.Scope.Children.Length > 0 || projection.Scope.Nested.Length > 0 || projection.Scope.Joins.Length > 0 ||
                projection.Scope.Removals.Length > 0 || projection.Scope.JoinRemovals.Length > 0)
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' targeting an event folds flat 'from' transitions only.");
            }

            if (projection.Scope.From.Any(from => !from.Key.Equals(SemanticProjectionKey.EventSourceIdentity)))
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' targeting an event publishes one instance per event source; its transitions key on the event source identity.");
            }

            if (projection.Scope.From.Any(from => from.ParentKey is not null))
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' targeting an event has no parent key.");
            }
        }
    }
}
