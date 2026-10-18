// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_every_declared_event_is_appended_and_registered : given.a_compiled_model
{
    void Because() => _result = Compare();

    [Fact] void should_establish_no_drift() => _result.HasDrift.ShouldEqual(false);
    [Fact] void should_be_complete() => _result.Complete.ShouldBeTrue();
    [Fact] void should_have_no_findings() => _result.Findings.ShouldBeEmpty();
    [Fact] void should_count_events_once() => _result.Coverage.AppendedEvents.ShouldEqual(2);
    [Fact] void should_show_attributed_events() => _result.Slices.Single().Producers.Single().Edges.Sum(value => value.AttributedEvents).ShouldEqual(2);
}
