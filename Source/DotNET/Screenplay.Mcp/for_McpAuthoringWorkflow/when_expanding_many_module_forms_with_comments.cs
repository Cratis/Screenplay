// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_many_module_forms_with_comments : given.an_authoring_connection
{
    static readonly (string Form, string[] Comments)[] _forms =
    [
        ("ShowProductForm", ["// @input show orderId", "// show product field"]),
        ("ChooseProductForm", ["// @input choose productId", "// choose quantity field"]),
        ("PlaceOrderForm", ["// @input place orderId", "// place quantity field"]),
        ("ChangeOrderForm", ["// @input change orderId", "// change product field"]),
        ("ReceiveStockForm", ["// @input receive productId", "// receive quantity field"]),
        ("CountStockForm", ["// @input count productId", "// count quantity field"])
    ];

    JsonElement _proposal;
    JsonElement _dropped;
    string _source = string.Empty;
    string _warehouseSource = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept OrderId : Uuid\nconcept ProductId : Uuid\n");
        Write("Shop/Shop.play", """
            module Shop
              form ShowProductForm for PlaceOrder
                // @input show orderId
                populate from item
                // show product field
                field productId label "Product"
              form ChooseProductForm for PlaceOrder
                // @input choose productId
                populate from item
                // choose quantity field
                field quantity label "Quantity"
            """);
        Write("Shop/Ordering/Ordering.play", """
            module Shop
              form PlaceOrderForm for PlaceOrder
                // @input place orderId
                populate from item
                // place quantity field
                field quantity label "Quantity"
              form ChangeOrderForm for PlaceOrder
                // @input change orderId
                populate from item
                // change product field
                field productId label "Product"
            """);
        Write("Shop/Ordering/PlaceOrder/PlaceOrder.play", """
            module Shop
              feature Ordering
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId OrderId identifier
                    productId ProductId
                    quantity Int
                    produces OrderPlaced
                      for orderId
                      productId = productId
                      quantity = quantity
                  event OrderPlaced
                    productId ProductId
                    quantity Int
            """);
        Write("Shop/Catalog/Products/Products.play", "module Shop\n  feature Catalog\n    slice StateView Products\n");
        Write("Warehouse/Warehouse.play", """
            module Warehouse
              form ReceiveStockForm for ReceiveStock
                // @input receive productId
                populate from item
                // receive quantity field
                field quantity label "Quantity"
              form CountStockForm for ReceiveStock
                // @input count productId
                populate from item
                // count quantity field
                field quantity label "Quantity"
            """);
        Write("Warehouse/Stock/Stock.play", "module Warehouse\n  feature Stock\n    slice StateView Inventory\n");
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Shop" });
        _proposal = Result("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "feature",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _dropped = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items");
        Apply(opened, _proposal);
        _source = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
        _warehouseSource = File.ReadAllText(Path.Combine(RootPath, "Warehouse", "Warehouse.play"));
    }

    [Fact]
    void should_keep_every_comment_once_inside_its_own_form()
    {
        foreach (var (form, comments) in _forms)
        {
            var source = form.EndsWith("StockForm", StringComparison.Ordinal) ? _warehouseSource : _source;
            var command = form.EndsWith("StockForm", StringComparison.Ordinal) ? "ReceiveStock" : "PlaceOrder";
            var start = source.IndexOf($"  form {form} for {command}", StringComparison.Ordinal);
            start.ShouldBeGreaterThan(-1);
            var end = source.IndexOf("\n  form ", start + 1, StringComparison.Ordinal);
            var body = source[start..(end < 0 ? source.Length : end)];
            foreach (var comment in comments)
            {
                body.ShouldContain($"    {comment}");
                source.Split(comment, StringSplitOptions.None).Length.ShouldEqual(2);
            }
        }
    }

    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_report_play0288() => _proposal.GetRawText().ShouldNotContain("PLAY0288");

    void Write(string path, string source)
    {
        var fullPath = Path.Combine(RootPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, source);
    }
}
