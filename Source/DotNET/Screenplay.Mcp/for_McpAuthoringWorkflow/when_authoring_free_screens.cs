// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// A shape, a screen declared free of any slice, a slice's adoption of it and the data the slice provides are typed syntax like
/// everything else a UI author writes: each is read as a node, a typed replace edits it and a typed remove deletes it.
/// </summary>
public class when_authoring_free_screens : given.an_authoring_connection
{
    const string Application = """
        shape Invoice
          number String
          amount Decimal

        screen InvoiceList
          data Invoice[]
          table Invoice
            column number

        module Invoicing
          feature Paying
            slice StateView ListInvoices
              readmodel InvoiceRow
                invoiceNumber String
                customer String
                amount Decimal
              query ListInvoices => InvoiceRow[]
              uses screen InvoiceList
                data Invoice via query ListInvoices
                  number from invoiceNumber
        """;

    JsonElement _opened;
    JsonElement[] _shapes = [];
    JsonElement[] _needs = [];
    JsonElement[] _adoptions = [];
    JsonElement[] _mappings = [];
    string _redescribed = string.Empty;
    string _remappedField = string.Empty;
    string _removedAdoption = string.Empty;
    string _removedShape = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        Apply(empty, Result("propose-source", ArgumentsFor(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = Application })));
        _opened = Open();
        var revision = _opened.GetProperty("revision").GetString()!;
        _shapes = Nodes("ShapeSyntax", revision);
        _needs = Nodes("ScreenNeedSyntax", revision);
        _adoptions = Nodes("UsesScreenSyntax", revision);
        _mappings = Nodes("ScreenProvisionMappingSyntax", revision);

        _redescribed = Text(Propose(Replacement(_shapes.Single(), node => node["description"] = "What the invoice list reads")));
        _remappedField = Text(Propose(Replacement(_mappings.Single(), node => node["path"] = "customer")));
        _removedAdoption = Text(Propose(new { operation = "remove", target = _adoptions.Single().GetProperty("handle") }));
        _removedShape = Text(Propose(new { operation = "remove", target = _shapes.Single().GetProperty("handle") }));
    }

    [Fact] void should_read_the_shape_as_a_typed_node() => _shapes.Length.ShouldEqual(1);
    [Fact] void should_read_the_need_as_a_typed_node() => _needs.Length.ShouldEqual(1);
    [Fact] void should_read_the_adoption_as_a_typed_node() => _adoptions.Length.ShouldEqual(1);
    [Fact] void should_read_the_mapping_as_a_typed_node() => _mappings.Length.ShouldEqual(1);
    [Fact] void should_replace_the_shape_description() => _redescribed.ShouldContain("shape Invoice\n  description \"What the invoice list reads\"");
    [Fact] void should_replace_a_mapping_path() => _remappedField.ShouldContain("number from customer");
    [Fact] void should_remove_the_adoption() => _removedAdoption.ShouldNotContain("uses screen InvoiceList");
    [Fact] void should_keep_the_screen_when_removing_the_adoption() => _removedAdoption.ShouldContain("screen InvoiceList\n  data Invoice[]");
    [Fact] void should_remove_the_shape() => _removedShape.ShouldNotContain("shape Invoice");

    JsonElement[] Nodes(string kind, string revision) => [.. Result("read-ast", new { expectedRevision = revision, kind, includeContent = true, limit = 200 })
        .GetProperty("page").GetProperty("items").EnumerateArray()];

    object Replacement(JsonElement target, Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(target.GetProperty("node").GetRawText())!.AsObject();
        edit(node);
        return new { operation = "replace", target = target.GetProperty("handle"), node };
    }

    static object ArgumentsFor(JsonElement opened, object document) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { document }
    };

    JsonElement Propose(object operation) => Result("propose-ast", ProposalArguments(operation));

    string Text(JsonElement proposal)
    {
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        var text = Candidate(proposal).Documents.Single().Text;
        Result("discard-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
        return text;
    }

    object ProposalArguments(object operation) => new
    {
        expectedRevision = _opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
        validation = "Authoring",
        formatting = "CanonicalizeTouchedDocuments",
        includeContent = true,
        operations = new[] { operation }
    };
}
