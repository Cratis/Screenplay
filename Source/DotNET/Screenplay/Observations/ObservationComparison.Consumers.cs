// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

internal sealed partial class ObservationComparisonRun
{
    void CompareConsumers()
    {
        foreach (var consumer in snapshot.Consumers.Where(value => value.Owner == ObservedConsumerOwner.Platform))
        {
            Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Excluded, "Platform-owned consumers are excluded.", consumer: consumer.Identifier, reason: ObservationUnknownReason.PlatformConsumer, sequence: consumer.SequenceId);
        }

        foreach (var consumer in snapshot.Consumers.Where(value => value.Owner != ObservedConsumerOwner.Platform && value.Kind is ObservedConsumerKind.Projection or ObservedConsumerKind.Reducer))
        {
            Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Unknown, "Projection and reducer consumer semantics are not compared.", consumer: consumer.Identifier, reason: ObservationUnknownReason.ConsumerKindNotCompared, sequence: consumer.SequenceId);
        }

        foreach (var slice in _index.ModelSlices)
        {
            foreach (var projection in slice.Projections)
            {
                Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Unknown, "Projection consumer semantics are not compared.", projection.Id, reason: ObservationUnknownReason.ConsumerKindNotCompared);
            }

            foreach (var reducer in slice.Reducers)
            {
                Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Unknown, "Reducer consumer semantics are not compared.", reducer.ReadModel, reason: ObservationUnknownReason.ConsumerKindNotCompared);
            }

            foreach (var reaction in slice.Reactions)
            {
                CompareReactionConsumers(reaction);
            }
        }
    }

    void CompareReactionConsumers(SemanticReaction reaction)
    {
        var mapped = map.Consumers.GetValueOrDefault(reaction.Id);
        if (mapped is null)
        {
            Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "The reaction has no explicit host consumer mapping.", reaction.Id, reason: ObservationUnknownReason.UnmappedConstruct);
        }
        else if (map.Consumers.Count(value => value.Value == mapped) > 1)
        {
            Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Unknown, "Several reactions map to this consumer identity.", reaction.Id, consumer: mapped, reason: ObservationUnknownReason.AmbiguousIdentity);
        }

        foreach (var trigger in reaction.Triggers.Where(value => value.Kind == SemanticReactionTriggerKind.Event).DistinctBy(value => value.Source))
        {
            CompareTriggerConsumer(reaction.Id, trigger.Source, mapped);
        }

        if (mapped is not null && ReactionBehind(reaction.Id))
        {
            Add(ObservationFindingKind.ConsumerMismatch, ObservationOutcome.Unknown, "The mapped consumer has not established progress through the captured tail.", reaction.Id, consumer: mapped, reason: ObservationUnknownReason.ConsumerBehind);
        }
    }

    void CompareTriggerConsumer(SemanticId reaction, SemanticId source, string? mapped)
    {
        var type = CurrentType(source);
        var candidates = snapshot.Consumers.Where(value => value.Owner == ObservedConsumerOwner.Application && value.Kind == ObservedConsumerKind.Reaction &&
            value.EventTypes.Contains(type!)).ToArray();
        var onSequence = candidates.Where(value => value.SequenceId == snapshot.Coverage.SequenceId).ToArray();
        var outcome = ObservationOutcome.Informational;
        ObservationUnknownReason? reason = null;
        if (Foreign(source))
        {
            outcome = ObservationOutcome.Excluded;
        }
        else if (type is null || AmbiguousEvent(source))
        {
            outcome = ObservationOutcome.Unknown;
            reason = type is null ? ObservationUnknownReason.UnmappedConstruct : ObservationUnknownReason.AmbiguousIdentity;
            Add(ObservationFindingKind.ConsumerMismatch, outcome, "The trigger event has no unambiguous stored type mapping.", reaction, type, consumer: mapped, reason: reason);
        }
        else if (onSequence.Length == 0)
        {
            outcome = MissingConsumerOutcome();
            reason = snapshot.Coverage.Complete ? null : ObservationUnknownReason.PartialCoverage;
            Add(candidates.Length == 0 ? ObservationFindingKind.ConsumerNotRegistered : ObservationFindingKind.ConsumerOnOtherSequence, outcome, candidates.Length == 0 ? "No application reaction consumer subscribes to the trigger event." : "Matching reaction consumers subscribe only on another sequence.", reaction, type, consumer: mapped, reason: reason);
        }
        else if (mapped is not null && MappedSubscriptionMismatch(reaction, mapped))
        {
            outcome = ObservationOutcome.Drift;
            Add(ObservationFindingKind.ConsumerMismatch, outcome, "The mapped consumer subscribes to events outside the reaction triggers.", reaction, type, consumer: mapped);
        }
        else if (mapped is not null && !onSequence.Any(value => value.Identifier == mapped))
        {
            outcome = MissingConsumerOutcome();
            reason = snapshot.Coverage.Complete ? null : ObservationUnknownReason.PartialCoverage;
            Add(ObservationFindingKind.ConsumerMismatch, outcome, "The mapped consumer subscription does not match the reaction triggers on the scanned sequence.", reaction, type, consumer: mapped, reason: reason);
        }
        else if (mapped is not null && map.Consumers.Count(value => value.Value == mapped) > 1)
        {
            outcome = ObservationOutcome.Unknown;
            reason = ObservationUnknownReason.AmbiguousIdentity;
        }
        else if (mapped is null || ReactionBehind(reaction))
        {
            outcome = ObservationOutcome.Unknown;
            reason = mapped is null ? ObservationUnknownReason.UnmappedConstruct : ObservationUnknownReason.ConsumerBehind;
        }

        if (!_consumers.TryGetValue(reaction, out var views))
        {
            _consumers[reaction] = views = [];
        }

        views.Add(new(reaction, source, [.. candidates.Select(value => new ConsumerReference(value.Identifier, value.SequenceId, value.NextSequenceNumber, value.LastHandledSequenceNumber))], outcome, reason));
    }

    ObservationOutcome MissingConsumerOutcome() => snapshot.Coverage.Complete ? ObservationOutcome.Drift : ObservationOutcome.Unknown;

    bool MappedSubscriptionMismatch(SemanticId reaction, string identifier)
    {
        var declaration = _index.ModelSlices.SelectMany(value => value.Reactions).Single(value => value.Id == reaction);
        var expected = declaration.Triggers.Where(value => value.Kind == SemanticReactionTriggerKind.Event).Select(value => CurrentType(value.Source)).OfType<ObservedEventType>().ToHashSet();

        return snapshot.Consumers.Any(value => value.Identifier == identifier && value.SequenceId == snapshot.Coverage.SequenceId &&
            value.Owner == ObservedConsumerOwner.Application && value.Kind == ObservedConsumerKind.Reaction && value.EventTypes.Any(type => !expected.Contains(type)));
    }

    bool ReactionBehind(SemanticId id)
    {
        if (!map.Consumers.TryGetValue(id, out var identifier) || snapshot.Coverage.CapturedTail is not { } tail) return false;
        var consumers = snapshot.Consumers.Where(value => value.Identifier == identifier && value.SequenceId == snapshot.Coverage.SequenceId &&
            value.Owner == ObservedConsumerOwner.Application && value.Kind == ObservedConsumerKind.Reaction).ToArray();

        return consumers.Length == 0 || consumers.Any(value => value.LastHandledSequenceNumber is null || value.LastHandledSequenceNumber < tail);
    }
}
