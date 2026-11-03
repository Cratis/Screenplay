// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

// 23:30Z on 2025-03-01 is already 2025-03-02 in Oslo (UTC+1) and still 2025-03-01 in UTC.
public class with_a_calendar_around_midnight : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _inOslo = null!;
    SemanticExecutionResult _inUtc = null!;

    void Establish() => Compile(Source("        calendar \"Europe/Oslo\"\n        validate\n          require dueDate == today\n        produces Done\n          for id\n"));

    void Because()
    {
        _inOslo = Run("2025-03-02", Instant);
        var utc = Source("        validate\n          require dueDate == today\n        produces Done\n          for id\n");
        Compile(utc);
        _inUtc = Run("2025-03-02", Instant);
    }

    [Fact] void should_accept_a_due_date_that_is_today_in_the_calendar_zone() => _inOslo.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_use_utc_only_when_no_calendar_is_declared() => _inUtc.ShouldBeOfExactType<SemanticRejected>();
}
