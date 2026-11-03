// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_competing_time_zone_sources : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("        calendar \"America/New_York\"\n", "Europe/Oslo", "Europe/Oslo", "2025-03-01")]
    [InlineData("        calendar \"Europe/Oslo\"\n", "America/New_York", "America/New_York", "2025-03-02")]
    [InlineData("        calendar from $context.timeZone\n", "Europe/Oslo", "Europe/Oslo", "2025-03-01")]
    [InlineData("        calendar from $context.timeZone\n", null, "Europe/Oslo", "2025-03-01")]
    [InlineData("", "America/New_York", "Europe/Oslo", "2025-03-01")]
    [InlineData("", "Europe/Oslo", "America/New_York", "2025-03-02")]
    [InlineData("", null, "America/New_York", "2025-03-01")]
    [InlineData("", null, null, "2025-03-02")]
    public void should_select_command_then_specification_then_configured_or_declared_application_zone(string calendar, string? scenario, string? configured, string expectedDate)
    {
        Compile(Source(calendar + "        validate\n          require dueDate == today\n        produces Done\n          for id\n", "time zone configurable default \"Asia/Tokyo\"\n"));
        Run(expectedDate, Instant, new() { ContextZone = "America/New_York", ScenarioZone = scenario, ConfiguredZone = configured }).ShouldBeOfExactType<SemanticAccepted>();
    }
}
