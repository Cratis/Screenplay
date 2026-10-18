// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_a_command_appends_an_undeclared_event : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Appends.Add(new(new("Unexpected", 1), 7));
        _snapshot.Edges.Add(new(Register, new("Unexpected", 1), 7));
    }

    void Because() => _result = Compare();

    [Fact] void should_report_the_undeclared_edge() => Finding(ObservationFindingKind.UndeclaredEdgeObserved).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_identify_the_producer() => Finding(ObservationFindingKind.UndeclaredEdgeObserved).Producer.ShouldEqual(Register);
    [Fact] void should_link_the_producing_slice() => Finding(ObservationFindingKind.UndeclaredEdgeObserved).Slice.ShouldEqual(Slice.Id);
    [Fact] void should_count_events_not_invocations() => Finding(ObservationFindingKind.UndeclaredEdgeObserved).Count.ShouldEqual(7);
}
