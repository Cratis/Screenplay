// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ExpressionParser;

public class when_parsing_structured_numeric_literals
{
    [Theory]
    [InlineData("[1e400]")]
    [InlineData("[-1e400]")]
    [InlineData("{\"amount\":1e400}")]
    public void should_reject_non_finite_structured_numbers(string source)
    {
        var context = ParserContext.ForDiagnostics();
        var expression = ExpressionParser.ParseMappingSource(context, source, SourceLocation.Start);
        Assert.IsType<RawExpressionSyntax>(expression);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_a_309_digit_structured_number()
    {
        var context = ParserContext.ForDiagnostics();
        var expression = ExpressionParser.ParseMappingSource(context, $"[1{new string('0', 309)}]", SourceLocation.Start);
        Assert.IsType<RawExpressionSyntax>(expression);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStructuredValue).ShouldBeTrue();
    }

    [Theory]
    [InlineData("12345678901234567", typeof(long))]
    [InlineData("9223372036854775809", typeof(decimal))]
    [InlineData("0.00001", typeof(decimal))]
    [InlineData("2", typeof(double))]
    public void should_share_scalar_numeric_classification_and_survive_printing(string text, Type expected)
    {
        var context = ParserContext.ForDiagnostics();
        var source = $"[{{\"amount\":{text}}}]";
        var parsed = Assert.IsType<ListExpressionSyntax>(ExpressionParser.ParseMappingSource(context, source, SourceLocation.Start));
        var value = Assert.IsType<LiteralExpressionSyntax>(Assert.IsType<ObjectExpressionSyntax>(parsed.Items.Single()).Members.Single().Value);
        Assert.IsType(expected, value.Value);
        var printed = ScreenplaySyntaxText.Expression(parsed);
        var reparsed = ExpressionParser.ParseMappingSource(ParserContext.ForDiagnostics(), printed, SourceLocation.Start);
        SyntaxJson.StructurallyEqual(parsed, reparsed).ShouldBeTrue();
        var json = SyntaxJson.Serialize(value).GetProperty("value");
        json.ValueKind.ShouldEqual(expected == typeof(double) ? JsonValueKind.Number : JsonValueKind.Object);

        var typed = JsonNode.Parse(SyntaxJson.Serialize(parsed).GetRawText())!;
        typed["items"]![0]!["members"]![0]!["value"]!["value"] = JsonNode.Parse(text);
        var admitted = Assert.IsType<ListExpressionSyntax>(SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(typed.ToJsonString())));
        var admittedValue = Assert.IsType<LiteralExpressionSyntax>(Assert.IsType<ObjectExpressionSyntax>(admitted.Items.Single()).Members.Single().Value);
        Assert.IsType<double>(admittedValue.Value).ShouldEqual(JsonSerializer.Deserialize<JsonElement>(text).GetDouble());
        SyntaxJson.StructurallyEqual(parsed, admitted).ShouldEqual(expected == typeof(double));
    }
}
