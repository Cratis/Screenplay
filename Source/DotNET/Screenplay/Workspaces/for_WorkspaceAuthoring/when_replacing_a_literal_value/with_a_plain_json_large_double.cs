// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_literal_value;

public class with_a_plain_json_large_double : given.a_document_with_literal_values
{
    void Because()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""{"kind":"LiteralExpressionSyntax","value":144115188075855872}""");
        var literal = (LiteralExpressionSyntax)SyntaxJson.Deserialize(json);
        Result = Propose(ReplaceSource("quantity", literal));
    }

    [Fact] void should_accept_the_double_from_plain_json() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_print_source_digits_that_reparse_to_the_same_double() =>
        Candidate().ShouldEqual(Bytes(OrderSource.Replace("quantity =   2", "quantity =   144115188075855872", StringComparison.Ordinal)));
}
