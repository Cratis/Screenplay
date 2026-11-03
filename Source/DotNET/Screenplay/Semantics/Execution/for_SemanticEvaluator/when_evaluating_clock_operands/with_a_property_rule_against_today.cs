// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_property_rule_against_today : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _today = null!;
    SemanticExecutionResult _yesterday = null!;

    void Establish() => Compile(Source("        calendar \"Europe/Oslo\"\n        validate\n          dueDate >= today\n        produces Done\n          for id\n"));

    void Because()
    {
        _today = Run("2025-03-02", Instant);
        _yesterday = Run("2025-03-01", Instant);
    }

    [Fact] void should_accept_the_calendar_day() => _today.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_refuse_a_date_before_the_calendar_day() => _yesterday.ShouldBeOfExactType<SemanticRejected>();
}
