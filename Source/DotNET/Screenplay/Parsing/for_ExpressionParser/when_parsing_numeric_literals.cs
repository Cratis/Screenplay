// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_parsing_numeric_literals
{
    [Theory]
    [InlineData("-1", typeof(double), "-1")]
    [InlineData("2", typeof(double), "2")]
    [InlineData("2.0", typeof(double), "2")]
    [InlineData("2.50", typeof(double), "2.5")]
    [InlineData("0.1", typeof(decimal), "0.1")]
    [InlineData("0.00001", typeof(decimal), "0.00001")]
    [InlineData("1e-5", typeof(decimal), "0.00001")]
    [InlineData("1e-6", typeof(decimal), "0.000001")]
    [InlineData("1e-28", typeof(decimal), "0.0000000000000000000000000001")]
    [InlineData("0.00000000000000000000000000001", typeof(double), "1E-29")]
    [InlineData("1e-3", typeof(decimal), "0.001")]
    [InlineData("2.5E+4", typeof(double), "25000")]
    [InlineData("1e17", typeof(double), "100000000000000000")]
    [InlineData("-0", typeof(double), "0")]
    [InlineData("144115188075855872", typeof(double), "144115188075855872")]
    [InlineData("18446744073709551616", typeof(double), "18446744073709551616")]
    [InlineData("0.1000000000000000055511151231257827021181583404541015625", typeof(double), "0.1000000000000000055511151231257827021181583404541015625")]
    [InlineData("1152921504606846976", typeof(double), "1152921504606846976")]
    [InlineData("100000000000000020", typeof(long), "100000000000000020")]
    [InlineData("9007199254740993", typeof(long), "9007199254740993")]
    [InlineData("9223372036854775809", typeof(decimal), "9223372036854775809")]
    [InlineData("9007199254740993.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", typeof(long), "9007199254740993")]
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
    [InlineData("9223372036854775809", "Decimal", "9223372036854775809")]
    public void should_preserve_precise_numbers_in_typed_json(string source, string expectedType, string expectedValue)
    {
        var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start)!;
        var number = SyntaxJson.Serialize(parsed).GetProperty("value");
        number.GetProperty("literalType").GetString().ShouldEqual(expectedType);
        number.GetProperty("value").GetString().ShouldEqual(expectedValue);
    }

    [Theory]
    [InlineData("2", "2")]
    [InlineData("2.0", "2")]
    [InlineData("2.50", "2.5")]
    [InlineData("1e17", "1E+17")]

    [InlineData("-0", "-0")]
    public void should_keep_chronicle_storage_text_and_json_shape_for_faithful_literals(string source, string stored)
    {
        var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start)!;

        // Mirrors Chronicle origin/main ProjectionDefinitionSyntaxVisitor.FormatLiteralForStorage.
        var formatted = parsed.Value is double number ? number.ToString(CultureInfo.InvariantCulture) :
            Convert.ToString(parsed.Value, CultureInfo.InvariantCulture);
        formatted.ShouldEqual(stored);
        SyntaxJson.Serialize(parsed).GetProperty("value").ValueKind.ShouldEqual(JsonValueKind.Number);
    }

    [Theory]
    [InlineData("144115188075855872")]
    [InlineData("18446744073709551616")]
    public void should_keep_faithful_large_binary_numbers_as_plain_json_numbers(string text)
    {
        var parsed = ExpressionParser.ParseLiteral(text, SourceLocation.Start)!;
        var value = Assert.IsType<double>(parsed.Value);
        var serialized = SyntaxJson.Serialize(parsed).GetProperty("value");
        serialized.ValueKind.ShouldEqual(JsonValueKind.Number);
        serialized.GetRawText().ShouldEqual(JsonSerializer.Serialize(value));
        var restored = Assert.IsType<double>(((LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed))).Value);
        BitConverter.DoubleToInt64Bits(restored).ShouldEqual(BitConverter.DoubleToInt64Bits(value));
    }

    [Theory]
    [InlineData("-0")]
    [InlineData("-0.0")]
    public void should_preserve_signed_zero_on_parse_and_keep_mains_printed_zero(string text)
    {
        var parsed = ExpressionParser.ParseLiteral(text, SourceLocation.Start)!;
        var number = Assert.IsType<double>(parsed.Value);
        BitConverter.DoubleToInt64Bits(number).ShouldEqual(BitConverter.DoubleToInt64Bits(-0d));
        ScreenplaySyntaxText.Expression(parsed).ShouldEqual("0");
    }

    [Fact]
    public void should_fall_back_to_double_when_decimal_would_round()
    {
        var parsed = ExpressionParser.ParseLiteral("0.123456789012345678901234567890", SourceLocation.Start)!;
        var number = Assert.IsType<double>(parsed.Value);
        var printed = ScreenplaySyntaxText.Expression(parsed);
        var reparsed = ExpressionParser.ParseLiteral(printed, SourceLocation.Start)!;
        Assert.IsType<double>(reparsed.Value).ShouldEqual(number);
        SyntaxJson.StructurallyEqual(parsed, reparsed).ShouldBeTrue();
    }

    [Fact]
    public void should_keep_an_underflowing_decimal_as_a_double()
    {
        var parsed = ExpressionParser.ParseLiteral("1e-130", SourceLocation.Start)!;
        Assert.IsType<double>(parsed.Value).ShouldEqual(1e-130);
        Assert.IsType<double>(ExpressionParser.ParseLiteral(ScreenplaySyntaxText.Expression(parsed), SourceLocation.Start)!.Value).ShouldEqual(1e-130);
    }

    [Fact]
    public void should_not_turn_an_invalid_non_finite_double_into_a_finite_number()
    {
        ScreenplaySyntaxText.Expression(new LiteralExpressionSyntax(double.NaN, SourceLocation.Start)).ShouldEqual("NaN");
    }

    [Fact]
    public void should_keep_a_double_underflow_that_main_admitted_as_zero()
    {
        var parsed = ExpressionParser.ParseLiteral("1e-324", SourceLocation.Start)!;
        Assert.IsType<double>(parsed.Value).ShouldEqual(0d);
    }

    [Fact]
    public void should_preserve_main_overflow_for_a_fixed_point_number()
    {
        var source = "1" + new string('0', 309);
        var parsed = ExpressionParser.ParseLiteral(source, SourceLocation.Start)!;
        Assert.IsType<double>(parsed.Value).ShouldEqual(double.PositiveInfinity);
        ScreenplaySyntaxText.Expression(parsed).ShouldEqual("Infinity");
    }
}
