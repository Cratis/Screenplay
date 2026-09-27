// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_an_ordinary_json_number : given.a_document_with_literal_values
{
    [Theory]
    [InlineData("2")]
    [InlineData("3.5")]
    [InlineData("0.00001")]
    [InlineData("0.1")]
    [InlineData("144115188075855872")]
    [InlineData("1e-29")]
    [InlineData("9007199254740993")]
    public void should_accept_a_trivia_preserving_numeric_edit(string text)
    {
        var literal = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{text}}}"));
        Assert.IsType<double>(literal.Value);
        var result = Propose(ReplaceSource("quantity", literal));
        Assert.True(result.Accepted, string.Join("; ", result.AuthoringDiagnostics.Select(diagnostic => diagnostic.Message)));
    }
}
