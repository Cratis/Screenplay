// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// An agent adds a toolbar to a feature, a button linked to a command of the feature and the confirmation it asks (Cratis/Screenplay#1012), through the
/// typed nodes: the toolbar is a member of the feature, the button a member of the toolbar. Each change is revision-checked, applied and read back from the file.
/// </summary>
public class when_authoring_a_feature_toolbar : given.an_authoring_connection
{
    const string Document = "module Shop\n  feature Orders\n    slice StateChange PlaceOrder\n      command PlaceOrder\n        orderId Uuid identifier\n        produces OrderPlaced\n          orderId = orderId\n      event OrderPlaced\n        orderId Uuid\n";

    string _toolbarAdded = string.Empty;
    string _confirmationAdded = string.Empty;
    string _buttonRemoved = string.Empty;
    string[] _schemaMembers = [];

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        Apply(empty, Result("propose-source", Arguments(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = Document })));

        _schemaMembers = [.. Result("syntax-schema", new { kind = "FeatureSyntax" }).GetProperty("properties").EnumerateObject().Select(property => property.Name)];

        var opened = Open();
        var feature = Nodes("FeatureSyntax", opened).Single();
        var toolbar = new JsonObject
        {
            ["kind"] = "ScreenToolbarSyntax",
            ["name"] = "actions",
            ["items"] = new JsonArray(),
            ["buttons"] = new JsonArray(new JsonObject { ["kind"] = "ToolbarButtonSyntax", ["id"] = "place", ["title"] = "Place order", ["slice"] = "Shop.Orders.PlaceOrder", ["emphasis"] = "Primary", ["icon"] = "plus" })
        };
        _toolbarAdded = ApplyChange(new { operation = "add", parent = feature.GetProperty("handle"), member = "toolbars", node = toolbar });

        opened = Open();
        var place = Nodes("ToolbarButtonSyntax", opened).Single();
        _confirmationAdded = ApplyChange(Replace(place, button => button["confirm"] = "Place it?"));

        opened = Open();
        _buttonRemoved = ApplyChange(new { operation = "remove", target = Nodes("ToolbarButtonSyntax", opened).Single().GetProperty("handle") });
    }

    [Fact] void should_expose_the_toolbars_of_a_feature_as_a_member() => _schemaMembers.ShouldContain("toolbars");
    [Fact] void should_add_the_toolbar_to_the_feature() => _toolbarAdded.ShouldContain("toolbar actions\n      button place title \"Place order\" slice Shop.Orders.PlaceOrder emphasis primary icon plus");
    [Fact] void should_add_the_confirmation_under_the_button() => _confirmationAdded.ShouldContain("icon plus\n        confirm \"Place it?\"");
    [Fact] void should_remove_the_button() => _buttonRemoved.ShouldNotContain("button place");

    JsonElement[] Nodes(string kind, JsonElement opened) => [.. Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind, includeContent = true, limit = 200 })
        .GetProperty("page").GetProperty("items").EnumerateArray()];

    static object Replace(JsonElement target, Action<JsonObject> edit)
    {
        var node = JsonNode.Parse(target.GetProperty("node").GetRawText())!.AsObject();
        edit(node);
        return new { operation = "replace", target = target.GetProperty("handle"), node };
    }

    static object Arguments(JsonElement opened, object document) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { document }
    };

    string ApplyChange(object operation)
    {
        var opened = Open();
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { operation }
        });
        Apply(opened, proposal);
        return File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }
}
