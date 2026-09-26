// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_parsing_numeric_literals
{
    [Theory]
    [InlineData("-1", typeof(long), "-1")]
    [InlineData("1.5", typeof(decimal), "1.5")]
    [InlineData("1e-3", typeof(decimal), "0.001")]
    [InlineData("2.5E+4", typeof(decimal), "25000.0")]
    [InlineData("9007199254740993", typeof(long), "9007199254740993")]
    [InlineData("9223372036854775808", typeof(decimal), "9223372036854775808")]
    [InlineData("1e-29", typeof(double), "1E-29")]
    public void should_round_trip_numeric_value_and_type(string source, Type expectedType, string expectedText)
    {
        var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start)!;
        Assert.IsType(expectedType, parsed.Value);
        var printed = ScreenplaySyntaxText.Expression(parsed);
        printed.ShouldEqual(expectedText);
        var reparsed = ExpressionParser.ParseLiteral(printed, SourceLocation.Start)!;
        Assert.IsType(expectedType, reparsed.Value);
        reparsed.Value.ShouldEqual(parsed.Value);
        SyntaxJson.StructurallyEqual(parsed, (LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("9007199254740993", "Int64", "9007199254740993")]
    [InlineData("9223372036854775808", "Decimal", "9223372036854775808")]
    [InlineData("1e-3", "Decimal", "0.001")]
    public void should_preserve_precise_numbers_in_typed_json(string source, string expectedType, string expectedValue)
    {
        var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start)!;
        var number = SyntaxJson.Serialize(parsed).GetProperty("value");
        number.GetProperty("literalType").GetString().ShouldEqual(expectedType);
        number.GetProperty("value").GetString().ShouldEqual(expectedValue);
    }

    [Fact]
    public void should_fall_back_to_double_when_decimal_would_round()
    {
        var parsed = ExpressionParser.ParseLiteral("0.123456789012345678901234567890", SourceLocation.Start)!;
        var number = Assert.IsType<double>(parsed.Value);
        var printed = ScreenplaySyntaxText.Expression(parsed);
        var reparsed = ExpressionParser.ParseLiteral(printed, SourceLocation.Start)!;
        Convert.ToDouble(reparsed.Value, System.Globalization.CultureInfo.InvariantCulture).ShouldEqual(number);
    }

    [Fact]
    public void should_reject_a_number_outside_the_finite_range()
    {
        var context = ParserContext.ForDiagnostics();
        ExpressionParser.ParseProjectionExpression(context, "1e400", SourceLocation.Start).ShouldBeOfExactType<RawExpressionSyntax>();
        context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidExpression);
    }
}
