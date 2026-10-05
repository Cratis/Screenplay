// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_the_clock_advances : given.a_v6_scenario
{
    const string Source =
        """
        module Billing
          feature Collections
            slice Automation Digests
              reaction Clock
                at 07:30 on Monday
                  produces DigestIssued
                    for "weekly"
                    issuedAt = $context.occurred
                every 6 hours
                  produces Ticked
                    for "ticks"
                    at = $context.occurred
                at 06:00 on day 5
                  produces DigestIssued
                    for "monthly"
                    issuedAt = $context.occurred
              event DigestIssued
                issuedAt DateTime
              event Ticked
                at DateTime
              specification ReachingMondayMorning
                given clock "2026-10-04T23:00:00Z"
                when clock "2026-10-05T07:30:00Z"
                then Ticked
                  for "ticks"
                  at = "2026-10-05T00:00:00Z"
                then Ticked
                  for "ticks"
                  at = "2026-10-05T06:00:00Z"
                then DigestIssued
                  for "monthly"
                  issuedAt = "2026-10-05T06:00:00Z"
                then DigestIssued
                  for "weekly"
                  issuedAt = "2026-10-05T07:30:00Z"
              specification StoppingShortOfTheSchedule
                given clock "2026-10-05T07:00:00Z"
                when clock "2026-10-05T07:29:59Z"
                then DigestIssued
                  for "weekly"
                  issuedAt = "2026-10-05T07:30:00Z"
        """;

    SemanticSpecificationRun _reached;
    SemanticSpecificationRun _short;

    void Establish() => Compile(Source);

    void Because()
    {
        _reached = Run("ReachingMondayMorning");
        _short = Run("StoppingShortOfTheSchedule");
    }

    [Fact] void should_fire_every_due_occurrence_once_in_time_order() => _reached.Passed.ShouldBeTrue();
    [Fact] void should_not_fire_what_is_not_yet_due() => _short.Failures.ShouldContain("Expected 1 fact(s), got 0.");
}
