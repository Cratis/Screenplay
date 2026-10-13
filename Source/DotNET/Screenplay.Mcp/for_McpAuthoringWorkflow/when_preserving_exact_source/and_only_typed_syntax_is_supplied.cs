// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_only_typed_syntax_is_supplied : given.an_exact_source_connection
{
    JsonElement _refused;

    void Because()
    {
        var command = Node("CommandSyntax", Opened.GetProperty("revision").GetString());
        var replacement = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var property = replacement["properties"]![1]!.DeepClone();
        property["name"] = "notes";
        replacement["properties"]!.AsArray().Add(property);
        _refused = Call("propose-ast", new
        {
            expectedRevision = Opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
            formatting = "PreserveExactSource",
            operations = new[] { new { operation = "replace", target = command.GetProperty("handle"), node = replacement } }
        }).GetProperty("result");
    }

    [Fact] void should_still_refuse_ast_edits_requiring_printing() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_require_canonical_printing_consent() => _refused.GetProperty("structuredContent").GetProperty("conflicts").GetRawText().ShouldContain("CanonicalizeTouchedDocuments");
    [Fact] void should_not_retain_a_proposal() => _refused.GetProperty("structuredContent").TryGetProperty("proposalId", out _).ShouldBeFalse();
}
