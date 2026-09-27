// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_round_tripping_condition_exponents : given.a_printer
{
    [Theory]
    [InlineData("1e20")]
    [InlineData("2.5E+4")]
    [InlineData("1e-3")]
    public void should_preserve_an_opaque_production_condition_operand(string number)
    {
        var source = $"""
            module Orders
              feature Placement
                slice StateChange Place
                  command PlaceOrder
                    amount Decimal
                    produces when amount == {number}
                      OrderPlaced
                        amount = amount
                  event OrderPlaced
                    amount Decimal
            """;
        var roundtrip = RoundTrip(source);
        Assert.Empty(roundtrip.Original!.Diagnostics);
        Assert.Empty(roundtrip.Reparsed.Diagnostics);
        Assert.Contains($"produces when amount == {number}", roundtrip.Printed);
        Assert.IsType<RawExpressionSyntax>(((ComparisonConditionSyntax)roundtrip.Original.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().When!).Right);
        Assert.True(SyntaxJson.StructurallyEqual(roundtrip.Original.Value, roundtrip.Reparsed.Value!));
        Assert.Equal(roundtrip.Printed, roundtrip.PrintedAgain);
    }

    [Theory]
    [InlineData("1e20")]
    [InlineData("2.5E+4")]
    [InlineData("1e-3")]
    public void should_preserve_an_opaque_policy_claim_target(string number)
    {
        var roundtrip = RoundTrip($"policy CheckAmount\n  require claim \"amount\" matches {number}");
        Assert.Empty(roundtrip.Original!.Diagnostics);
        Assert.Empty(roundtrip.Reparsed.Diagnostics);
        Assert.Contains($"require claim \"amount\" matches {number}", roundtrip.Printed);
        Assert.IsType<RawExpressionSyntax>(((ClaimConditionSyntax)roundtrip.Original.Value!.Policies.Single().Condition!).Matches);
        Assert.True(SyntaxJson.StructurallyEqual(roundtrip.Original.Value, roundtrip.Reparsed.Value!));
        Assert.Equal(roundtrip.Printed, roundtrip.PrintedAgain);
    }
}
