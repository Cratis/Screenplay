// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_round_tripping_exponent_like_operands
{
    [Theory]
    [InlineData("1e3-4")]
    [InlineData("1e20foo")]
    [InlineData("123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("1e+3")]
    public void should_keep_condition_operands_whole(string operand)
    {
        var source = $"module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n        produces when amount == {operand}\n          OrderPlaced\n            amount = amount\n      event OrderPlaced\n        amount Decimal\n";
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(source);
        Assert.True(parsed.Success, string.Join("; ", parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var condition = (ComparisonConditionSyntax)parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().When!;
        Assert.Equal(operand, ((RawExpressionSyntax)condition.Right).Text);
        var reparsed = compiler.Parse(new ScreenplayPrinter().Print(parsed.Value));
        Assert.True(reparsed.Success, string.Join("; ", reparsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var again = (ComparisonConditionSyntax)reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().When!;
        Assert.Equal(operand, ((RawExpressionSyntax)again.Right).Text);
    }

    [Theory]
    [InlineData("1e20foo")]
    [InlineData("1e+20")]
    public void should_keep_policy_targets_whole(string operand)
    {
        var source = $"policy AmountPolicy\n  require claim \"amount\" matches {operand}\n";
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(source);
        Assert.True(parsed.Success, string.Join("; ", parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var claim = (ClaimConditionSyntax)parsed.Value!.Policies.Single().Condition!;
        Assert.Equal(operand, ((RawExpressionSyntax)claim.Matches!).Text);
        var reparsed = compiler.Parse(new ScreenplayPrinter().Print(parsed.Value));
        Assert.True(reparsed.Success, string.Join("; ", reparsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var again = (ClaimConditionSyntax)reparsed.Value!.Policies.Single().Condition!;
        Assert.Equal(operand, ((RawExpressionSyntax)again.Matches!).Text);
    }
}
