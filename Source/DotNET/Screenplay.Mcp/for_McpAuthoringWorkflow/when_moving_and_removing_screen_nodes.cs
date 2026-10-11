// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// One transcript per screen node kind: read its typed handle, propose a typed remove or move, apply, and read the
/// file back. Each transcript starts from the same applied model so no edit depends on another.
/// </summary>
public class when_moving_and_removing_screen_nodes : given.an_authoring_connection
{
    const string Application = """
        layout Shell
          outlet main
          navigation contributes Navigation
          content

        ui profile Web
          target platform web
          layout Shell

          packages
            scene.web
            Cratis.Components

          icons
            scene.icons
            workspace.icons

        module Sales
          screen template Browser
            outlet detail
            list
            detail

          dialog template Confirm
            body

          dialog template Alert
            body

          form RegisterForm for Register
            field invoiceId label "Invoice"
            field amount label "Amount"

          contribute to Navigation
            navigate to Browse
            label "Browse"
            icon folder

          contribute to Navigation
            navigate to Browse
            label "Archive"

          feature Invoices
            slice StateChange Register
              command Register
                invoiceId String identifier
                amount Decimal

              screen Register
                title "Register"

            slice StateView Browse
              screen Browse
                template Browser
                component scene.web.DataGrid invoices
                  id "invoices:list"
                  property selected from data selected
                  property title = "Invoices"
                  outlet detail
                    title "Selected"
                toolbar primary
                  item register navigate to Register
                  item archive navigate to Browse
                navigate to Browse
                  outlet main
                title "Invoices"
        """;

    JsonElement _opened;
    JsonElement _missingPackage;
    readonly Dictionary<string, string> _results = new(StringComparer.Ordinal);

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        Apply(empty, Result("propose-source", Arguments(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = Application })));
        _opened = Open();
        var revision = _opened.GetProperty("revision").GetString()!;

        Transcript("remove screen", () => Remove(Nodes("ScreenSyntax", revision).Single(node => Name(node) == "Register")));
        Transcript("remove screen template", () => Remove(Nodes("ScreenTemplateSyntax", revision).Single()));
        Transcript("remove dialog template", () => Remove(Nodes("DialogTemplateSyntax", revision).First()));
        Transcript("remove form", () => Remove(Nodes("FormSyntax", revision).Single()));
        Transcript("remove binding", () => Remove(Nodes("ComponentPropertySyntax", revision).First()));
        Transcript("remove component outlet", () => Remove(Nodes("ComponentOutletSyntax", revision).Single()));
        Transcript("remove navigation", () => Remove(Nodes("ScreenNavigateSyntax", revision).Single(node => node.GetProperty("node").TryGetProperty("outlet", out var outlet) && outlet.ValueKind == JsonValueKind.String)));
        Transcript("remove navigation contribution", () => Remove(Nodes("ContributionSyntax", revision).Last()));
        Transcript("remove toolbar", () => Remove(Nodes("ScreenToolbarSyntax", revision).Single()));
        Transcript("remove ui profile", () => Remove(Nodes("UiProfileSyntax", revision).Single()));
        Transcript("move form field", () => Move(Nodes("FormFieldSyntax", revision).Last(), Nodes("FormSyntax", revision).Single(), "fields", 0));
        Transcript("move component property", () => Move(Nodes("ComponentPropertySyntax", revision).Last(), Nodes("ScreenComponentSyntax", revision).Single(), "properties", 0));
        Transcript("move toolbar item", () => Move(Nodes("ToolbarItemSyntax", revision).Last(), Nodes("ScreenToolbarSyntax", revision).Single(), "items", 0));
        Transcript("move navigation contribution", () => Move(Nodes("ContributionSyntax", revision).Last(), Nodes("ModuleSyntax", revision).Single(), "contributions", 0));
        Transcript("move screen directive", () => Move(Nodes("ScreenTitleSyntax", revision).Last(), Nodes("ScreenSyntax", revision).Single(node => Name(node) == "Browse"), "directives", 0));
        Transcript("move dialog template", () => Move(Nodes("DialogTemplateSyntax", revision).Last(), Nodes("ModuleSyntax", revision).Single(), "dialogTemplates", 0));
        Transcript("move package", () => MoveValue("packages", ["Cratis.Components", "scene.web"]));
        Transcript("move icon", () => MoveValue("icons", ["workspace.icons", "scene.icons"]));
        Transcript("remove package", () => MoveValue("packages", ["scene.web"]));
        _missingPackage = Call("propose-ast", ProposalArguments(ProfileReplacement("packages", ["Cratis.Components"]))).GetProperty("result");
        Transcript("remove icon", () => MoveValue("icons", ["scene.icons"]));
            }

    [Fact] void should_remove_a_screen() => Text("remove screen").ShouldNotContain("screen Register");
    [Fact] void should_keep_the_slice_when_removing_its_screen() => Text("remove screen").ShouldContain("slice StateChange Register");
    [Fact] void should_remove_a_screen_template() => Text("remove screen template").ShouldNotContain("screen template Browser");
    [Fact] void should_remove_a_dialog_template() => Text("remove dialog template").ShouldNotContain("dialog template Confirm");
    [Fact] void should_remove_a_form() => Text("remove form").ShouldNotContain("form RegisterForm");
    [Fact] void should_remove_a_binding() => Text("remove binding").ShouldNotContain("property selected from data selected");
    [Fact] void should_remove_a_component_outlet() => Text("remove component outlet").ShouldNotContain("title \"Selected\"");
    [Fact] void should_remove_a_navigation() => Text("remove navigation").ShouldNotContain("outlet main\n        title");
    [Fact] void should_remove_a_navigation_contribution() => Text("remove navigation contribution").ShouldNotContain("label \"Archive\"");
    [Fact] void should_remove_a_toolbar() => Text("remove toolbar").ShouldNotContain("toolbar primary");
    [Fact] void should_remove_a_ui_profile() => Text("remove ui profile").ShouldNotContain("ui profile Web");
    [Fact] void should_move_a_form_field() => Before("move form field", "field amount", "field invoiceId").ShouldBeTrue();
    [Fact] void should_move_a_component_property() => Before("move component property", "property title = \"Invoices\"", "property selected").ShouldBeTrue();
    [Fact] void should_move_a_toolbar_item() => Before("move toolbar item", "item archive", "item register").ShouldBeTrue();
    [Fact] void should_move_a_navigation_contribution() => Before("move navigation contribution", "label \"Archive\"", "label \"Browse\"").ShouldBeTrue();
    [Fact] void should_move_a_screen_directive() => Before("move screen directive", "screen Browse\n        title \"Invoices\"", "        template Browser").ShouldBeTrue();
    [Fact] void should_move_a_dialog_template() => Before("move dialog template", "dialog template Alert", "dialog template Confirm").ShouldBeTrue();
    [Fact] void should_move_a_package() => Before("move package", "Cratis.Components", "scene.web").ShouldBeTrue();
    [Fact] void should_move_an_icon() => Before("move icon", "workspace.icons", "scene.icons").ShouldBeTrue();
    [Fact] void should_remove_an_unused_package() => Text("remove package").ShouldNotContain("Cratis.Components");
    [Fact] void should_refuse_to_remove_a_package_a_component_still_uses() => _missingPackage.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_missing_package() => _missingPackage.GetRawText().ShouldContain("PLAY0632");
    [Fact] void should_name_the_component_whose_package_is_missing() => _missingPackage.GetRawText().ShouldContain("scene.web.DataGrid");
    [Fact] void should_apply_every_transcript() => _results.Where(entry => !entry.Value.StartsWith("module", StringComparison.Ordinal) && !entry.Value.StartsWith("layout", StringComparison.Ordinal)).Select(entry => entry.Key).ShouldBeEmpty();
    [Fact] void should_remove_an_icon() => Text("remove icon").ShouldNotContain("workspace.icons");

    void Transcript(string name, Func<JsonElement> propose)
    {
        JsonElement proposal;
        try
        {
            proposal = propose();
        }
        catch (Exception exception)
        {
            _results[name] = $"failed: {exception.Message}";
            return;
        }

        if (!proposal.GetProperty("success").GetBoolean())
        {
            _results[name] = $"refused: {proposal.GetRawText()}";
            return;
        }

        _results[name] = Candidate(proposal).Documents.Single().Text;
        Result("discard-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
    }

    string Text(string name)
    {
        var text = _results[name];
        if (text.StartsWith("failed:", StringComparison.Ordinal) || text.StartsWith("refused:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Transcript '{name}' did not produce a proposal: {text}");
        }

        return text;
    }

    bool Before(string name, string first, string second)
    {
        var text = Text(name);
        var firstIndex = text.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = text.IndexOf(second, StringComparison.Ordinal);
        return firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex;
    }

    JsonElement Remove(JsonElement node) => Propose(new { operation = "remove", target = node.GetProperty("handle") });

    JsonElement Move(JsonElement node, JsonElement parent, string member, int index) =>
        Propose(new { operation = "move", target = node.GetProperty("handle"), parent = parent.GetProperty("handle"), member, index });

    JsonElement MoveValue(string member, string[] values) => Propose(ProfileReplacement(member, values));

    object ProfileReplacement(string member, string[] values)
    {
        var profile = Nodes("UiProfileSyntax", _opened.GetProperty("revision").GetString()!).Single();
        var node = JsonSerializer.Deserialize<Dictionary<string, object?>>(profile.GetProperty("node").GetRawText())!;
        node[member] = values;
        return new { operation = "replace", target = profile.GetProperty("handle"), node };
    }

    JsonElement Propose(object operation) => Result("propose-ast", ProposalArguments(operation));

    object ProposalArguments(object operation) => new
    {
        expectedRevision = _opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
        validation = "Authoring",
        formatting = "CanonicalizeTouchedDocuments",
        includeContent = true,
        operations = new[] { operation }
    };

    IEnumerable<JsonElement> Nodes(string kind, string revision) => Result("read-ast", new { expectedRevision = revision, kind, includeContent = true, limit = 200 })
        .GetProperty("page").GetProperty("items").EnumerateArray().ToArray();

    static string? Name(JsonElement node) => node.GetProperty("node").TryGetProperty("name", out var name) ? name.GetString() : null;

    static object Arguments(JsonElement opened, object document) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { document }
    };
}
