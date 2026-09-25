// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_forms_with_comments : given.an_authoring_connection
{
    const string Application = """
        concept OrderId : Uuid
        concept ProductId : Uuid
        """;

    const string Module = """
        module Shop
          description "Shop module"
          // Comment above the module's own form
          form ShowProductForm for PlaceOrder
            // @input orderId — module-file form input
            populate from item
            field productId label "Product"
        """;

    const string Feature = """
        module Shop
          // Module-level comment above the form
          form PlaceOrderForm for PlaceOrder
            // @input orderId — new identity supplied by the client
            // @input productId — selected product row
            populate from item
            field quantity label "Quantity"
        """;

    const string Slice = """
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
        """;

    JsonElement _proposal;
    JsonElement _dropped;
    string _moduleSource = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Application);
        Write("Shop/Shop.play", Module);
        Write("Shop/Ordering/Ordering.play", Feature);
        Write("Shop/Ordering/PlaceOrder/PlaceOrder.play", Slice);
        Write("Shop/Catalog/Products/Products.play", "module Shop\n  feature Catalog\n    slice StateView Products\n");
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
        _moduleSource = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
    }

    [Fact] void should_keep_feature_form_comments_inside_the_form_in_order() => _moduleSource.ShouldContain("  // Module-level comment above the form\n  form PlaceOrderForm for PlaceOrder\n    // @input orderId — new identity supplied by the client\n    // @input productId — selected product row\n    populate from item");
    [Fact] void should_keep_module_file_form_comments_inside_the_form_in_order() => _moduleSource.ShouldContain("  // Comment above the module's own form\n  form ShowProductForm for PlaceOrder\n    // @input orderId — module-file form input\n    populate from item");
    [Fact] void should_not_duplicate_module_file_comment() => _moduleSource.Split("// @input orderId — module-file form input", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_disclose_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);

    void Write(string path, string source)
    {
        var fullPath = Path.Combine(RootPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, source);
    }
}
