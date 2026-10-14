// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_exhausting_the_position_budget : Specification
{
    [Theory]
    [InlineData("^(((x?){99}){99}){99}$", 36)]
    [InlineData("^((x?){2000}){2000}$", 1024)]
    void should_bound_nested_nullable_repeat_unrolling(string pattern, int length)
    {
        var expression = new RegexFragmentR(pattern, length).Parse();
        var error = Catch.Exception(() => new GlushkovAutomaton(expression));
        error.ShouldBeOfExactType<RegexStateBudgetExceeded>();
        error!.Message.ShouldEqual("position budget exceeded");
    }

    [Fact]
    void should_count_positions_not_the_authored_repeat_nodes()
    {
        var expression = RegexExpression.Repeat(RegexExpression.Character("a"), GlushkovAutomaton.PositionBudget + 1, GlushkovAutomaton.PositionBudget + 1);
        Catch.Exception(() => new GlushkovAutomaton(expression)).ShouldBeOfExactType<RegexStateBudgetExceeded>();
    }

    [Fact]
    void should_admit_exactly_the_position_budget()
    {
        var expression = RegexExpression.Repeat(RegexExpression.Character("a"), GlushkovAutomaton.PositionBudget, GlushkovAutomaton.PositionBudget);
        Catch.Exception(() => new GlushkovAutomaton(expression)).ShouldBeNull();
    }
}
