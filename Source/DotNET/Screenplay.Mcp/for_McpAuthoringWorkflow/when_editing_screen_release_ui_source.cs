// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_editing_screen_release_ui_source : given.an_authoring_connection
{
    const string Application = "module Sales\n  feature Invoices\n    import \"invoices.play\"\n";
    const string Original = """
        slice StateView BrowseInvoices
          query ListInvoices => InvoiceList[]

          readmodel InvoiceList
            invoiceId String

          screen BrowseInvoices
            toolbar main
              item details navigate to InvoiceDetails
                parameter invoiceId from component invoiceGrid.selected.invoiceId
            component Cratis.Components.DataGrid invoiceGrid
              context from query ListInvoices
              property selectedItem from component invoiceGrid.selected null preserve
              exposes selectedInvoice from component invoiceGrid.selected
        """;

    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    string _applicationBefore = string.Empty;
    string _invoiceBefore = string.Empty;
    string _invoiceAfter = string.Empty;
    string _documentIdBefore = string.Empty;
    string _documentIdAfter = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _opened = Open();
        Apply(_opened, Result("propose-source", Arguments(_opened, [
            new { operation = "create-document", path = "application.play", stableKey = "application", source = Application },
            new { operation = "create-document", path = "invoices.play", stableKey = "invoices", source = Original }
        ])));

        _opened = Open();
        _applicationBefore = File.ReadAllText(Path.Combine(RootPath, "application.play"));
        _invoiceBefore = File.ReadAllText(Path.Combine(RootPath, "invoices.play"));
        _documentIdBefore = DocumentId(_opened, "invoices.play");
        var edited = _invoiceBefore
            .Replace("property selectedItem from component invoiceGrid.selected null preserve", "property selectedItem from component invoiceGrid.checkedItem null clear", StringComparison.Ordinal)
            .Replace("item details navigate to InvoiceDetails", "item details dialog InvoiceDetailsDialog", StringComparison.Ordinal);
        _proposal = Result("propose-source", Arguments(_opened, new { operation = "replace-document", documentId = _documentIdBefore, source = edited }));
        Apply(_opened, _proposal);
        _applied = Open();
        _invoiceAfter = File.ReadAllText(Path.Combine(RootPath, "invoices.play"));
        _documentIdAfter = DocumentId(_applied, "invoices.play");
    }

    [Fact] void should_propose_the_revision_checked_edit() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_document_identity() => _documentIdAfter.ShouldEqual(_documentIdBefore);
    [Fact] void should_preserve_unrelated_files() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(_applicationBefore);
    [Fact] void should_modify_the_component_output_binding() => _invoiceAfter.ShouldContain("from component invoiceGrid.checkedItem null clear");
    [Fact] void should_modify_the_toolbar_action() => _invoiceAfter.ShouldContain("item details dialog InvoiceDetailsDialog");
    [Fact] void should_keep_the_stable_component_instance_id() => _invoiceAfter.ShouldContain("component Cratis.Components.DataGrid invoiceGrid");
    [Fact] void should_read_the_edited_typed_binding_ast() => ComponentProperty().GetProperty("binding").GetProperty("componentPropertyPath").GetString().ShouldEqual("checkedItem");
    [Fact] void should_read_the_edited_toolbar_target() => ToolbarItem().GetProperty("target").GetString().ShouldEqual("InvoiceDetailsDialog");

    JsonElement ComponentProperty() => ReadAst("ComponentPropertySyntax")
        .EnumerateArray().Single(item => item.GetProperty("node").GetProperty("property").GetString() == "selectedItem").GetProperty("node");

    JsonElement ToolbarItem() => ReadAst("ToolbarItemSyntax")
        .EnumerateArray().Single(item => item.GetProperty("node").GetProperty("name").GetString() == "details").GetProperty("node");

    JsonElement ReadAst(string kind) => Result("read-ast", new { expectedRevision = _applied.GetProperty("revision").GetString(), documentId = _documentIdAfter, kind, includeContent = true })
        .GetProperty("page").GetProperty("items");

    string DocumentId(JsonElement opened, string path) => Page("documents", opened.GetProperty("revision").GetString())
        .EnumerateArray().Single(document => document.GetProperty("path").GetString() == path).GetProperty("documentId").GetString()!;

    JsonElement Result(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");

    object Arguments(JsonElement opened, object document) => Arguments(opened, [document]);

    static object Arguments(JsonElement opened, object[] documents) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents
    };
}
