// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// An agent gives a module an extending outlet - empty at first, as the editor's Add toolbar does - and then adds a button linked to a command of one of the
/// module's features through the typed nodes (Cratis/StudioIssues#1083): the outlet is a member of the module, the button an item of the outlet.
/// </summary>
public class when_authoring_a_module_outlet : given.an_authoring_connection
{
    const string Document = "module Shop\n  screen template Page\n    category Page\n    header\n    outlet actions of actions in header\n  feature Orders\n    slice StateChange PlaceOrder\n      command PlaceOrder\n        orderId Uuid identifier\n        produces OrderPlaced\n          orderId = orderId\n      event OrderPlaced\n        orderId Uuid\n";

    string _emptyToolbarAdded = string.Empty;
    string _buttonAdded = string.Empty;
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

        _schemaMembers = [.. Result("syntax-schema", new { kind = "ModuleSyntax" }).GetProperty("properties").EnumerateObject().Select(property => property.Name)];

        var opened = Open();
        var module = Nodes("ModuleSyntax", opened).Single();
        var outlet = new JsonObject { ["kind"] = "OutletSyntax", ["name"] = "actions", ["mode"] = "Extend", ["items"] = new JsonArray() };
        _emptyToolbarAdded = ApplyChange(new { operation = "add", parent = module.GetProperty("handle"), member = "outlets", node = outlet });

        opened = Open();
        var added = Nodes("OutletSyntax", opened).Single(node => node.GetProperty("node").GetProperty("mode").GetString() == "Extend");
        var button = new JsonObject { ["kind"] = "ToolbarButtonSyntax", ["id"] = "place", ["title"] = "Place order", ["slice"] = "Orders.PlaceOrder", ["emphasis"] = "Primary", ["icon"] = "plus" };
        _buttonAdded = ApplyChange(new { operation = "add", parent = added.GetProperty("handle"), member = "items", node = button });
    }

    [Fact] void should_expose_the_outlets_of_a_module_as_a_member() => _schemaMembers.ShouldContain("outlets");
    [Fact] void should_add_the_outlet_to_the_module_without_a_button() => _emptyToolbarAdded.ShouldContain("  outlet actions extends\n");
    [Fact] void should_add_a_button_linked_to_the_command_of_a_feature_of_the_module() => _buttonAdded.ShouldContain("button place title \"Place order\" slice Orders.PlaceOrder emphasis primary icon plus");

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
