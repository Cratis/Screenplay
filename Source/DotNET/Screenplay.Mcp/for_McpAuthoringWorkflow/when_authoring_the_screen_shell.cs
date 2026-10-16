// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// One revision-checked transcript per construct of the application shell, run on the canonical shell vector: navigation
/// regions (switching a module's mode, copying the application's items, hierarchical items), toolbar buttons that invoke
/// State Change slices and blueprint template references. Each reads the typed nodes, proposes a typed edit, refuses a stale
/// revision, applies and reopens the workspace; each rule a proposal breaks is refused with the compiler's own diagnostic.
/// Sample data packs keep their own transcript in <c>when_authoring_sample_data</c>.
/// </summary>
public class when_authoring_the_screen_shell : given.an_authoring_connection
{
    JsonElement _staleNavigation;
    JsonElement _staleToolbar;
    JsonElement _staleTemplate;
    JsonElement _uncopiedOverride;
    JsonElement _retargetedToMissingModule;
    JsonElement _buttonOnStateView;
    JsonElement _invalidRange;
    JsonElement _unlistedBlueprint;
    string _overrideAdded = string.Empty;
    string _switchedToExtend = string.Empty;
    string _inheritingRegionExtended = string.Empty;
    string _childAdded = string.Empty;
    string _childRemoved = string.Empty;
    string _buttonAdded = string.Empty;
    string _buttonSliceChanged = string.Empty;
    string _buttonRemoved = string.Empty;
    string _moduleTemplateRange = string.Empty;
    string _screenTemplateRange = string.Empty;
    string _reopened = string.Empty;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        var shell = ScreenCompositionCorpus.V1.TypedSourceCases.Single(sourceCase => sourceCase.Name == "screen-shell-navigation-positive").Document.Text;
        Apply(empty, Result("propose-source", Arguments(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = shell })));
        var created = empty.GetProperty("revision").GetString()!;

        // Navigation regions.
        var opened = Open();
        _staleNavigation = Attempt(opened, Remove(Nodes("NavigationItemSyntax", opened).First()), created);
        _uncopiedOverride = Attempt(opened, AddRegion(opened, "Reports", region => region["mode"] = "Override"));
        _overrideAdded = ApplyChange(AddRegion(opened, "Reports", region =>
        {
            region["mode"] = "Override";
            Retarget(region["items"]!.AsArray(), "Reports", "Monthly");
        }));

        opened = Open();
        _switchedToExtend = ApplyChange(Replace(Region(opened, "Support", "Sidebar"), region => region["mode"] = "Extend"));

        opened = Open();
        _inheritingRegionExtended = ApplyChange(Replace(Region(opened, "Sales", "Topbar"), region =>
        {
            region["mode"] = "Extend";
            region["items"] = new JsonArray(Item("sales-reports", "Reports", "Sales"));
        }));

        opened = Open();
        var support = Nodes("NavigationItemSyntax", opened).Single(node => Id(node) == "support" && node.GetProperty("parent").GetProperty("path").GetString()!.StartsWith("/layouts/0/navigation/1", StringComparison.Ordinal));
        _childAdded = ApplyChange(new { operation = "add", parent = support.GetProperty("handle"), member = "children", node = Item("support-tickets", "Tickets", "Support") });

        opened = Open();
        var tickets = Nodes("NavigationItemSyntax", opened).Single(node => Id(node) == "support-tickets" && node.GetProperty("parent").GetProperty("path").GetString()!.StartsWith("/layouts/0/navigation/1", StringComparison.Ordinal));
        _retargetedToMissingModule = Attempt(opened, Replace(tickets, item => item["target"]!.AsObject()["module"] = "Ghost"));
        _childRemoved = ApplyChange(Remove(tickets));

        // Toolbar buttons.
        var navigationRevision = opened.GetProperty("revision").GetString()!;
        opened = Open();
        _staleToolbar = Attempt(opened, Remove(Nodes("ToolbarButtonSyntax", opened).First()), navigationRevision);
        var toolbar = Nodes("ScreenToolbarSyntax", opened).Single();
        _buttonAdded = ApplyChange(new { operation = "add", parent = toolbar.GetProperty("handle"), member = "buttons", node = Button("export", "Export", null) });

        opened = Open();
        var export = Nodes("ToolbarButtonSyntax", opened).Single(node => Id(node) == "export");
        _buttonOnStateView = Attempt(opened, Replace(export, button => button["slice"] = "Sales.Orders.OrderList"));
        _buttonSliceChanged = ApplyChange(Replace(export, button => button["slice"] = "Sales.Orders.CancelOrder"));

        opened = Open();
        _buttonRemoved = ApplyChange(Remove(Nodes("ToolbarButtonSyntax", opened).Single(node => Id(node) == "export")));

        // Template references.
        var toolbarRevision = opened.GetProperty("revision").GetString()!;
        opened = Open();
        _staleTemplate = Attempt(opened, Remove(Nodes("TemplateAssignmentSyntax", opened).Single()), toolbarRevision);
        var assignment = Nodes("TemplateAssignmentSyntax", opened).Single();
        _invalidRange = Attempt(opened, Replace(assignment, template => template["version"] = "latest"));
        _unlistedBlueprint = Attempt(opened, Replace(assignment, template => template["blueprint"] = "Hr"));
        _moduleTemplateRange = ApplyChange(Replace(assignment, template => template["version"] = "^2.0"));

        opened = Open();
        _screenTemplateRange = ApplyChange(Replace(Nodes("ScreenTemplateReferenceSyntax", opened).Single(node => node.GetProperty("node").TryGetProperty("blueprint", out var blueprint) && blueprint.GetString() == "Crm"), template => template["version"] = "~1.5.0"));

        _ = Open();
        _reopened = File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }

    [Fact] void should_refuse_a_stale_navigation_revision() => _staleNavigation.GetRawText().ShouldContain("StaleRevision");
    [Fact] void should_refuse_a_stale_toolbar_revision() => _staleToolbar.GetRawText().ShouldContain("StaleRevision");
    [Fact] void should_refuse_a_stale_template_revision() => _staleTemplate.GetRawText().ShouldContain("StaleRevision");
    [Fact] void should_refuse_to_copy_application_items_that_target_other_modules() => _uncopiedOverride.GetRawText().ShouldContain("PLAY0750");
    [Fact] void should_add_an_override_region_with_the_copied_items_retargeted_to_the_module() => _overrideAdded.ShouldContain("navigation sidebar overrides\n    item sales title \"Sales\" icon receipt target Reports.Monthly");
    [Fact] void should_keep_the_hierarchy_when_copying_items() => _overrideAdded.ShouldContain("      item orders title \"Orders\" target Reports.Monthly");
    [Fact] void should_switch_a_module_region_from_override_to_extend() => _switchedToExtend.ShouldContain("navigation sidebar extends\n    item tickets title \"Tickets\" target Support.Tickets");
    [Fact] void should_extend_an_inheriting_region_by_adding_its_first_item() => _inheritingRegionExtended.ShouldContain("navigation topbar extends\n    item sales-reports title \"Reports\" target Sales");
    [Fact] void should_add_a_sub_item_to_an_application_item() => _childAdded.ShouldContain("target Support\n      item support-tickets title \"Tickets\" target Support");
    [Fact] void should_refuse_an_item_that_targets_a_missing_module() => _retargetedToMissingModule.GetRawText().ShouldContain("PLAY0748");
    [Fact] void should_remove_a_sub_item() => _childRemoved.ShouldNotContain("item support-tickets");
    [Fact] void should_add_a_toolbar_button_without_a_slice() => _buttonAdded.ShouldContain("button export title \"Export\"");
    [Fact] void should_refuse_a_button_that_names_a_state_view_slice() => _buttonOnStateView.GetRawText().ShouldContain("PLAY0762");
    [Fact] void should_point_a_button_at_a_state_change_slice() => _buttonSliceChanged.ShouldContain("button export title \"Export\" slice Sales.Orders.CancelOrder");
    [Fact] void should_remove_a_toolbar_button() => _buttonRemoved.ShouldNotContain("button export");
    [Fact] void should_refuse_an_invalid_version_range() => _invalidRange.GetRawText().ShouldContain("PLAY0721");
    [Fact] void should_refuse_a_blueprint_no_ui_profile_lists() => _unlistedBlueprint.GetRawText().ShouldContain("PLAY0722");
    [Fact] void should_change_the_version_range_of_a_module_template() => _moduleTemplateRange.ShouldContain("template blueprint Crm Workspace version \"^2.0\"");
    [Fact] void should_change_the_version_range_of_a_screen_template() => _screenTemplateRange.ShouldContain("template blueprint Crm CustomerDetail version \"~1.5.0\"");
    [Fact] void should_keep_every_applied_edit_after_reopening() => new[] { "navigation sidebar overrides", "navigation sidebar extends", "item sales-reports", "version \"^2.0\"", "version \"~1.5.0\"" }.Where(text => !_reopened.Contains(text, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_keep_the_sample_data_the_ui_profile_selects() => _reopened.ShouldContain("sample data\n    active NordicDemo");

    static string Id(JsonElement node) => node.GetProperty("node").GetProperty("id").GetString()!;

    static JsonObject Item(string id, string title, string module) => new()
    {
        ["kind"] = "NavigationItemSyntax",
        ["id"] = id,
        ["title"] = title,
        ["target"] = new JsonObject { ["kind"] = "NavigationTargetSyntax", ["module"] = module }
    };

    static JsonObject Button(string id, string title, string? slice)
    {
        var button = new JsonObject { ["kind"] = "ToolbarButtonSyntax", ["id"] = id, ["title"] = title };
        if (slice is not null)
        {
            button["slice"] = slice;
        }

        return button;
    }

    static void Retarget(JsonArray items, string module, string feature)
    {
        foreach (var item in items.Select(entry => entry!.AsObject()))
        {
            item["target"] = new JsonObject { ["kind"] = "NavigationTargetSyntax", ["module"] = module, ["feature"] = feature };
            item.Remove("slot");
            if (item["children"] is JsonArray children)
            {
                Retarget(children, module, feature);
            }
        }
    }

    static object Remove(JsonElement node) => new { operation = "remove", target = node.GetProperty("handle") };

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

    JsonElement[] Nodes(string kind, JsonElement opened) => [.. Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind, includeContent = true, limit = 200 })
        .GetProperty("page").GetProperty("items").EnumerateArray()];

    JsonElement Region(JsonElement opened, string module, string region)
    {
        var modulePath = Nodes("ModuleSyntax", opened).Single(node => node.GetProperty("name").GetString() == module).GetProperty("handle").GetProperty("path").GetString();
        return Nodes("NavigationRegionSyntax", opened).Single(node => node.GetProperty("parent").GetProperty("path").GetString() == modulePath && node.GetProperty("node").GetProperty("region").GetString() == region);
    }

    object AddRegion(JsonElement opened, string module, Action<JsonObject> edit)
    {
        var parent = Nodes("ModuleSyntax", opened).Single(node => node.GetProperty("name").GetString() == module);
        var application = Nodes("NavigationRegionSyntax", opened).Single(node => node.GetProperty("parent").GetProperty("path").GetString()!.StartsWith("/layouts", StringComparison.Ordinal) && node.GetProperty("node").GetProperty("region").GetString() == "Sidebar");
        var region = JsonNode.Parse(application.GetProperty("node").GetRawText())!.AsObject();
        edit(region);
        return new { operation = "add", parent = parent.GetProperty("handle"), member = "navigation", node = region };
    }

    /// <summary>Proposes the operation against the given revision (the opened one unless a stale one is passed) and returns the refusal, or an accepted marker once it has discarded the proposal.</summary>
    JsonElement Attempt(JsonElement opened, object operation, string? revision = null)
    {
        var response = Call("propose-ast", new
        {
            expectedRevision = revision ?? opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { operation }
        });
        var result = response.GetProperty("result");
        if (response.TryGetProperty("error", out var error))
        {
            return error;
        }

        var structured = result.GetProperty("structuredContent");
        if (result.GetProperty("isError").GetBoolean())
        {
            return structured;
        }

        Result("discard-proposal", new { proposalId = structured.GetProperty("proposalId").GetString() });
        return JsonSerializer.SerializeToElement(new { accepted = true });
    }

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
