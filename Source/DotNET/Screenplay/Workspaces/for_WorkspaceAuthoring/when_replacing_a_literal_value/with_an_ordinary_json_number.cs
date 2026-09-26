// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_an_ordinary_json_number : given.a_document_with_literal_values
{
    [Theory]
    [InlineData("2", typeof(long))]
    [InlineData("3.5", typeof(decimal))]
    [InlineData("1e-29", typeof(double))]
    public void should_accept_a_trivia_preserving_numeric_edit(string text, Type kind)
    {
        var literal = (LiteralExpressionSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{text}}}"));
        Assert.IsType(kind, literal.Value);
        var result = Propose(ReplaceSource("quantity", literal));
        Assert.True(result.Accepted, string.Join("; ", result.AuthoringDiagnostics.Select(diagnostic => diagnostic.Message)));
    }
}
