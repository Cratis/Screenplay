// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison.when_coverage_is_partial;

public class and_an_undeclared_type_is_appended : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Coverage = _snapshot.Coverage with { CapturedTail = 20 };
        _snapshot.Appends.Add(new(new("Unexpected", 1), 1));
        _snapshot.Edges.Add(new(Register, new("Unexpected", 1), 1));
    }

    void Because() => _result = Compare();

    [Fact] void should_retain_presence_drift() => Finding(ObservationFindingKind.UndeclaredEventAppended).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_report_known_drift_despite_incomplete_coverage() => _result.HasDrift.ShouldEqual(true);
    [Fact] void should_keep_coverage_incomplete() => _result.Complete.ShouldBeFalse();
}
