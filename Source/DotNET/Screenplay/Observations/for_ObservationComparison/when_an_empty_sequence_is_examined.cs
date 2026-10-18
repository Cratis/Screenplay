// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_empty_sequence_is_examined : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Coverage = new("log", false, null, null, null, []);
        _snapshot.Appends = [];
        _snapshot.Edges = [];
    }

    void Because() => _result = Compare();

    [Fact] void should_be_complete() => _result.Complete.ShouldBeTrue();
    [Fact] void should_not_claim_the_producer_failed_to_run() => Finding(ObservationFindingKind.NoMatchingAppendsWithinCoverage).Outcome.ShouldEqual(ObservationOutcome.Informational);
    [Fact] void should_report_no_drift() => _result.HasDrift.ShouldEqual(false);
}
