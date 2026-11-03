// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_clock_operands_in_command_conditions : given.a_clock_binder
{
    const string Body = "        validate\n          require dueDate >= today - 7 days\n        produces when date of occurred <= date of now + 2 hours\n          Done\n            for id\n";

    CompilationResult<ClockOperandsBinding> _result = null!;

    void Because() => _result = BindClock(Source(Body));

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_the_working_major_37() => _result.Value!.LanguageVersion.Major.ShouldEqual(37u);
    [Fact] void should_select_the_working_semantic_major_37() => _result.Value!.SemanticVersion.Major.ShouldEqual(37u);

    [Fact]
    void should_bind_today_minus_seven_days_as_a_date_operand()
    {
        var comparison = (SemanticComparison)Command(_result.Value!).Requirements.Single().Condition;
        comparison.Right.Clock.ShouldEqual(new SemanticClockOperand(SemanticClockKind.Today, -7, SemanticClockUnit.Day));
    }

    [Fact]
    void should_bind_date_of_a_property_as_a_date_of_operand()
    {
        var comparison = (SemanticComparison)Command(_result.Value!).Produces.Single().When!;
        comparison.Left.DateOf.ShouldBeTrue();
    }

    [Fact]
    void should_bind_date_of_now_plus_two_hours()
    {
        var comparison = (SemanticComparison)Command(_result.Value!).Produces.Single().When!;
        comparison.Right.Clock.ShouldEqual(new SemanticClockOperand(SemanticClockKind.Now, 2, SemanticClockUnit.Hour) { DateOf = true });
    }
}
