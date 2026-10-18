// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_reaction_is_mapped;

public class and_its_reactor_is_behind : given.a_compiled_model
{
    void Establish()
    {
        Reaction(mapped: true);
        _snapshot.Consumers = [_snapshot.Consumers[0] with { LastHandledSequenceNumber = 0, NextSequenceNumber = 1 }];
        _snapshot.Appends = [new(Registered, 1)];
        _snapshot.Edges = [new(new(ObservedProducerKind.Reaction, "follow-host"), Registered, 1)];
    }

    void Because() => _result = Compare();

    [Fact] void should_disclose_consumer_progress() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldEqual(ObservationUnknownReason.ConsumerBehind);
    [Fact] void should_not_call_a_missing_edge_drift_before_catching_up() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Outcome.ShouldEqual(ObservationOutcome.Unknown);
    [Fact] void should_keep_positions_visible() => _result.Slices.Single().Consumers.Single().References.Single().LastHandledSequenceNumber.ShouldEqual(0);
}
