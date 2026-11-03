// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_calendar_from_the_context : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _supplied = null!;
    SemanticExecutionResult _missing = null!;
    SemanticExecutionResult _unknown = null!;
    SemanticExecutionResult _noOccurrence = null!;

    void Establish() => Compile(Source("        calendar from $context.timeZone\n        validate\n          require dueDate >= today\n        produces Done\n          for id\n"));

    void Because()
    {
        _supplied = Run("2025-03-02", Instant, new() { ContextZone = "Europe/Oslo" });
        _missing = Run("2025-03-02", Instant);
        _unknown = Run("2025-03-02", Instant, new() { ContextZone = "Mars/Olympus" });
        _noOccurrence = Run("2025-03-02", Instant, new() { ContextZone = "Europe/Oslo", WithOccurrence = false });
    }

    [Fact] void should_use_the_supplied_zone() => _supplied.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_fall_back_to_utc_when_the_zone_is_missing() => _missing.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_not_fall_back_to_utc_when_the_zone_is_unknown() => _unknown.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_refuse_a_command_without_an_occurrence() => _noOccurrence.ShouldBeOfExactType<SemanticRejected>();
}
