// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_specification_pinned_zone : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _pinned = null!;
    SemanticExecutionResult _unpinned = null!;
    SemanticExecutionResult _pinnedOverConfigured = null!;
    SemanticExecutionResult _calendarOverPin = null!;

    void Because()
    {
        Compile(Source("        validate\n          require dueDate >= today\n        produces Done\n          for id\n", "time zone configurable default \"Asia/Tokyo\"\n"));
        _pinned = Run("2025-03-01", Instant, new() { ScenarioZone = "America/New_York" });
        _unpinned = Run("2025-03-01", Instant);
        _pinnedOverConfigured = Run("2025-03-01", Instant, new() { ConfiguredZone = "Asia/Tokyo", ScenarioZone = "America/New_York" });
        Compile(Source("        calendar \"Asia/Tokyo\"\n        validate\n          require dueDate >= today\n        produces Done\n          for id\n"));
        _calendarOverPin = Run("2025-03-01", Instant, new() { ScenarioZone = "America/New_York" });
    }

    [Fact] void should_let_the_pin_beat_the_application_zone() => _pinned.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_use_the_application_zone_without_a_pin() => _unpinned.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_let_the_pin_beat_the_configured_zone() => _pinnedOverConfigured.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_let_the_command_calendar_beat_the_pin() => _calendarOverPin.ShouldBeOfExactType<SemanticRejected>();
}
