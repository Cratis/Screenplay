// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_numeric_json_literals
{
    [Theory]
    [InlineData("2", typeof(double))]
    [InlineData("3.5", typeof(double))]
    [InlineData("0.00001", typeof(decimal))]
    [InlineData("1e-6", typeof(decimal))]
    [InlineData("1e-28", typeof(decimal))]
    [InlineData("0.1", typeof(decimal))]
    [InlineData("1e-29", typeof(double))]
    [InlineData("9007199254740993", typeof(long))]
    [InlineData("9223372036854775809", typeof(decimal))]
    public void should_classify_plain_json_numbers_as_source_literals(string text, Type expected)
    {
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{text}}}"));
        Assert.IsType(expected, value.Value);
    }

    [Theory]
    [InlineData("3.5", "3.5")]
    [InlineData("0.1", "0.1000000000000000055511151231257827021181583404541015625")]
    [InlineData("1.4411518807585587E+17", "144115188075855872")]
    public void should_preserve_an_explicit_double_envelope(string text, string exact)
    {
        var source = $"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{{\"literalType\":\"Double\",\"value\":\"{text}\"}}}}";
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(source));
        var number = Assert.IsType<double>(value.Value);
        var serialized = SyntaxJson.Serialize(value).GetProperty("value");
        serialized.ValueKind.ShouldEqual(JsonValueKind.Number);
        serialized.GetRawText().ShouldEqual(exact);
        Assert.IsType<double>(((LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(value))).Value).ShouldEqual(number);
    }
}
