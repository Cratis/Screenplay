// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_editing_an_ordinary_json_number : given.an_authoring_connection
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Orders\n  feature Placement\n    slice StateChange Place\n      command PlaceOrder\n        amount Decimal\n      specification PlaceOrderSpec\n        when PlaceOrder\n          amount = 1\n");
        Initialize();
    }

    [Theory]
    [InlineData("0.1", "PreserveTrivia")]
    [InlineData("9.99", "PreserveTrivia")]
    [InlineData("2", "PreserveTrivia")]
    [InlineData("3.5", "PreserveTrivia")]
    [InlineData("0.00001", "PreserveTrivia")]
    [InlineData("9007199254740993", "PreserveTrivia")]
    [InlineData("0.1", "CanonicalizeTouchedDocuments")]
    [InlineData("9.99", "CanonicalizeTouchedDocuments")]
    [InlineData("2", "CanonicalizeTouchedDocuments")]
    [InlineData("3.5", "CanonicalizeTouchedDocuments")]
    [InlineData("0.00001", "CanonicalizeTouchedDocuments")]
    [InlineData("9007199254740993", "CanonicalizeTouchedDocuments")]
    public void should_accept_legacy_plain_json_numbers_in_ast_proposals(string text, string formatting)
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var mapping = Result("read-ast", new { expectedRevision = revision, kind = "PropertyMappingSyntax", includeContent = true })
            .GetProperty("page").GetProperty("items").EnumerateArray().Single();
        var node = JsonNode.Parse(mapping.GetProperty("node").GetRawText())!;
        node["source"]!["value"] = JsonNode.Parse(text);
        var proposal = Result("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting,
            validation = "Authoring",
            operations = new[] { new { operation = "replace", target = mapping.GetProperty("handle"), node } }
        });
        Assert.False(string.IsNullOrEmpty(proposal.GetProperty("proposalId").GetString()));
        var candidate = Candidate(proposal).Documents.Single().Text;
        if (text != "9007199254740993")
        {
            Assert.Contains($"amount = {text}", candidate, StringComparison.Ordinal);
        }
    }
}
