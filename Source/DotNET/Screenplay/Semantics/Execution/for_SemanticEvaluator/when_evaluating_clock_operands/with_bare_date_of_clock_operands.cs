// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_bare_date_of_clock_operands : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("now")]
    [InlineData("today")]
    public void should_require_the_calendar_date_of_the_bare_clock(string clock)
    {
        Compile(Source($"        calendar \"Europe/Oslo\"\n        validate\n          require dueDate == date of {clock}\n        produces Done\n          for id\n"));
        Run("2025-03-02", Instant).ShouldBeOfExactType<SemanticAccepted>();
        Run("2025-03-01", Instant).ShouldBeOfExactType<SemanticRejected>();
    }

    [Theory]
    [InlineData("now")]
    [InlineData("today")]
    public void should_produce_only_on_the_calendar_date_of_the_bare_clock(string clock)
    {
        Compile(Source($"        calendar \"Europe/Oslo\"\n        produces when dueDate == date of {clock}\n          Done\n            for id\n"));
        var matching = Run("2025-03-02", Instant);
        var other = Run("2025-03-01", Instant);
        matching.ShouldBeOfExactType<SemanticAccepted>();
        other.ShouldBeOfExactType<SemanticAccepted>();
        ((SemanticAccepted)matching).Facts.Length.ShouldEqual(1);
        ((SemanticAccepted)other).Facts.ShouldBeEmpty();
    }
}
