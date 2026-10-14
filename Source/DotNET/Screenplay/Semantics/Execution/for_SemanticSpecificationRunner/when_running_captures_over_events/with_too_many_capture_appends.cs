// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_too_many_capture_appends : given.an_event_capture
{
    void Because() => RunCapture(string.Join('\n', Enumerable.Repeat("append Added", SemanticReactionLoop.MaximumFacts)), """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Added
        """);

    [Fact] void should_respect_the_scenario_fact_budget() => _run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_keep_exactly_the_allowed_prefix() => _run.Execution.World.Facts.Length.ShouldEqual(SemanticReactionLoop.MaximumFacts);
}
