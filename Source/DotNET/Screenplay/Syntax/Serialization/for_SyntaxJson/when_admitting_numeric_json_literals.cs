// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_numeric_json_literals
{
    [Theory]
    [InlineData("2", 2d)]
    [InlineData("3.5", 3.5d)]
    [InlineData("0.00001", 0.00001d)]
    [InlineData("1e-6", 1e-6d)]
    [InlineData("1e-28", 1e-28d)]
    [InlineData("0.1", 0.1d)]
    [InlineData("1e-29", 1e-29d)]
    [InlineData("9007199254740993", 9007199254740992d)]
    [InlineData("9223372036854775809", 9223372036854775808d)]
    [InlineData("1.4411518807585587E+17", 144115188075855872d)]
    [InlineData("144115188075855870", 144115188075855872d)]
    public void should_read_plain_json_numbers_as_double(string text, double expected)
    {
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{text}}}"));
        Assert.IsType<double>(value.Value).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("3.5", 3.5d)]
    [InlineData("0.1", 0.1d)]
    [InlineData("1.4411518807585587E+17", 144115188075855872d)]
    public void should_preserve_an_explicit_double_envelope(string text, double expected)
    {
        var source = $"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{{\"literalType\":\"Double\",\"value\":\"{text}\"}}}}";
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(source));
        var number = Assert.IsType<double>(value.Value);
        number.ShouldEqual(expected);
        var serialized = SyntaxJson.Serialize(value).GetProperty("value");
        serialized.ValueKind.ShouldEqual(JsonValueKind.Number);
        serialized.GetRawText().ShouldEqual(JsonSerializer.Serialize(number));
        Assert.IsType<double>(((LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(value))).Value).ShouldEqual(number);
    }
}
