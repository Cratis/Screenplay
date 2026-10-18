// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_a_producer_is_not_in_the_model : given.a_compiled_model
{
    void Establish() => _snapshot.Edges = [new(new(ObservedProducerKind.Command, "unknown"), Registered, 1), new(Register, Audited, 1)];
    void Because() => _result = Compare();

    [Fact] void should_report_an_unmapped_producer() => _result.Findings.Single(value => value.Reason == ObservationUnknownReason.UnmappedProducer).Producer!.Identifier.ShouldEqual("unknown");
    [Fact] void should_not_call_the_unmapped_edge_drift() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.UndeclaredEdgeObserved).ShouldBeFalse();
}
