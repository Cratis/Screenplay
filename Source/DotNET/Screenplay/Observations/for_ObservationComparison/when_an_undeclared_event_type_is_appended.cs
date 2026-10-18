// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_an_undeclared_event_type_is_appended : given.a_compiled_model
{
    void Establish()
    {
        _snapshot.Appends.Add(new(new("Unexpected", 1), 4));
        _snapshot.Edges.Add(new(Register, new("Unexpected", 1), 4));
    }

    void Because() => _result = Compare();

    [Fact] void should_report_drift() => Finding(ObservationFindingKind.UndeclaredEventAppended).Outcome.ShouldEqual(ObservationOutcome.Drift);
    [Fact] void should_preserve_the_event_count() => Finding(ObservationFindingKind.UndeclaredEventAppended).Count.ShouldEqual(4);
    [Fact] void should_put_undeclared_types_last() => _result.Findings[^1].Construct.ShouldBeNull();
}
