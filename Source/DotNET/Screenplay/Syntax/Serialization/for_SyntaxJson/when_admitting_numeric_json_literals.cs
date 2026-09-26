// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_numeric_json_literals
{
    [Theory]
    [InlineData("2", typeof(long))]
    [InlineData("3.5", typeof(decimal))]
    [InlineData("1e-29", typeof(double))]
    public void should_classify_plain_json_numbers_as_source_literals(string text, Type expected)
    {
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{text}}}"));
        Assert.IsType(expected, value.Value);
    }

    [Fact]
    public void should_preserve_an_explicit_double_envelope()
    {
        const string source = "{\"kind\":\"LiteralExpressionSyntax\",\"value\":{\"literalType\":\"Double\",\"value\":\"3.5\"}}";
        var value = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(source));
        Assert.IsType<double>(value.Value).ShouldEqual(3.5d);
        ((LiteralExpressionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(value))).Value.ShouldEqual(3.5d);
    }
}
