// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_validation_rule_against_today : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _past = null!;
    SemanticExecutionResult _future = null!;
    SemanticExecutionResult _yesterday = null!;

    void Establish() => Compile(Source("        validate\n          require dueDate >= today\n            message \"Due date cannot be in the past\"\n        produces Done\n          for id\n"));

    void Because()
    {
        _past = Run("2025-03-01", Instant);
        _future = Run("2025-03-02", Instant);
        _yesterday = Run("2025-02-28", Instant);
    }

    [Fact] void should_compare_with_the_utc_day_when_no_calendar_is_declared() => _past.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_refuse_a_date_before_the_utc_day() => _yesterday.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_accept_a_later_date() => _future.ShouldBeOfExactType<SemanticAccepted>();
}
