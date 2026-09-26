// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_round_tripping_numeric_projection_literals : given.a_printer
{
    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("1e-3")]
    [InlineData("2.5E+4")]
    [InlineData("9007199254740993")]
    [InlineData("9223372036854775808")]
    [InlineData("0.123456789012345678901234567890")]
    [InlineData("18446744073709551616.000000000000000000000000000001")]
    [InlineData("1e-130")]
    public void should_preserve_the_literal_value_and_stabilize_printing(string number)
    {
        var source = $"module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        id String\n    slice StateView Lookup\n      readmodel Summary\n        id String\n        quantity Decimal\n      projection SummaryProjection => Summary\n        from Registered key id\n          quantity = {number}\n";
        var roundtrip = RoundTrip(source);
        Assert.True(roundtrip.Reparsed.Success, string.Join("; ", roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
        Value(roundtrip.Reparsed.Value!).ShouldEqual(Value(roundtrip.Original!.Value!));
        SyntaxJson.StructurallyEqual(roundtrip.Original.Value!, roundtrip.Reparsed.Value!).ShouldBeTrue();
    }

    static object? Value(ApplicationSyntax application) => ((LiteralExpressionSyntax)application.Modules.Single().Features.Single()
        .Slices.Single(slice => slice.Projections.Any()).Projections.Single().Blocks.OfType<FromSyntax>().Single()
        .Mappings.OfType<SetMappingSyntax>().Single().Source).Value;
}
