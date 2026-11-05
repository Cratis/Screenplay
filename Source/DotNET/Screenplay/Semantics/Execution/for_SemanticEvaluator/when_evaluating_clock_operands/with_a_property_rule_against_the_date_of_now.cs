// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_property_rule_against_the_date_of_now : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _calendarDay;
    SemanticExecutionResult _utcDay;

    void Establish() => Compile(Source("        calendar \"Europe/Oslo\"\n        validate\n          dueDate >= date of now\n        produces Done\n          for id\n"));

    void Because()
    {
        _calendarDay = Run("2025-03-02", Instant);
        _utcDay = Run("2025-03-01", Instant);
    }

    [Fact] void should_accept_the_calendar_date_of_now() => _calendarDay.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_refuse_a_date_before_the_calendar_date_of_now() => _utcDay.ShouldBeOfExactType<SemanticRejected>();
}
