// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_coverage_has_an_unexamined_gap : given.a_compiled_model
{
    void Establish() => _snapshot.Coverage = new("log", true, 0, 9, 9, [new(2, 3)]);
    void Because() => _result = Compare();

    [Fact] void should_report_partial_coverage() => _result.Findings.Any(value => value.Reason == ObservationUnknownReason.PartialCoverage).ShouldBeTrue();
    [Fact] void should_not_claim_no_drift() => _result.HasDrift.ShouldBeNull();
    [Fact] void should_keep_the_gap_visible() => _result.Coverage.Sequence.Unexamined.Single().ShouldEqual(new ObservedRange(2, 3));
}
