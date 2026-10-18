// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_a_declared_edge_is_not_observed;

public class and_the_command_appended_nothing : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Appends = [];
        _snapshot.Edges = [];
        _snapshot.Coverage = new("log", false, null, null, null, []);
    }

    void Because() => _result = Compare();

    [Fact] void should_report_no_evidence_of_execution() => Finding(ObservationFindingKind.NoMatchingAppendsWithinCoverage).Outcome.ShouldEqual(ObservationOutcome.Informational);
    [Fact] void should_accept_empty_sequence_coverage() => _result.Coverage.Sequence.Complete.ShouldBeTrue();
    [Fact] void should_not_claim_drift() => _result.HasDrift.ShouldEqual(false);
}
