// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_edge_is_not_observed;

public class and_the_command_appended_other_events : given.a_compiled_model
{
    void Establish() => OnlyAudited();
    void Because() => _result = Compare();

    [Fact] void should_report_the_missing_unconditional_edge() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_not_invent_an_uncertainty() => Finding(ObservationFindingKind.DeclaredEdgeNotObserved).Reason.ShouldBeNull();
    [Fact] void should_keep_the_slice_view_consistent() => _result.Slices.Single().Producers.Single().Edges[0].Outcome.ShouldEqual(ObservationOutcome.Drift);
}
