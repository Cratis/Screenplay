// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_edge_is_not_observed;

public class and_some_events_lack_causation : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Edges = [new(Register, Audited, 1)];
        _snapshot.Unattributed = [new(Registered, 1)];
    }

    void Because() => _result = Compare();

    [Fact] void should_not_treat_missing_causation_as_missing_production() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldEqual(ObservationUnknownReason.UnattributedEvents);
    [Fact] void should_count_unattributed_events_once() => _result.Coverage.UnattributedEvents.ShouldEqual(1);
    [Fact] void should_leave_the_verdict_unknown() => _result.HasDrift.ShouldBeNull();
}
