// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_authoring_nested_sample_rows : given.an_authoring_connection
{
    const string Application = """
        type Line
          sku String
          quantity Int

        module Shop
          feature Baskets
            slice StateView BasketList
              readmodel Basket
                lines Line[]

        sample data Demo
          shape Basket
            row
              lines
                row
                  sku = "BOOK"
                  quantity = 2
                row
                  sku = "PEN"
                  quantity = 3
        """;

    JsonElement _collection;
    string _changed = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        Apply(empty, Result("propose-source", new
        {
            expectedRevision = empty.GetProperty("revision").GetString(),
            expectedCatalogRevision = empty.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "create-document", path = "application.play", stableKey = "application", source = Application } }
        }));
        var opened = Open();
        var fields = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind = "SampleDataFieldSyntax", includeContent = true, limit = 200 })
            .GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        _collection = fields.Single(field => field.GetProperty("node").GetProperty("name").GetString() == "lines").GetProperty("node");
        var target = fields.First(field => field.GetProperty("node").GetProperty("name").GetString() == "sku");
        var node = JsonNode.Parse(target.GetProperty("node").GetRawText())!.AsObject();
        node["value"] = "BAG";
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments",
            includeContent = true,
            operations = new[] { new { operation = "replace", target = target.GetProperty("handle"), node } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        _changed = Candidate(proposal).Documents.Single().Text;
        Result("discard-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
    }

    [Fact] void should_read_the_nested_rows() => _collection.GetProperty("rows").GetArrayLength().ShouldEqual(2);
    [Fact] void should_read_the_collection_kind() => _collection.GetProperty("syntaxKind").GetString().ShouldEqual("Rows");
    [Fact] void should_replace_a_child_value() => _changed.ShouldContain("sku = \"BAG\"");
    [Fact] void should_keep_the_other_field() => _changed.ShouldContain("quantity = 2");
    [Fact] void should_keep_the_sibling_row() => _changed.ShouldContain("sku = \"PEN\"\n          quantity = 3");
}
