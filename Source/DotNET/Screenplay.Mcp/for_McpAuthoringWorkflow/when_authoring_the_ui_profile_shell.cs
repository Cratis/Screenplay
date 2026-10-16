// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

/// <summary>
/// One revision-checked transcript for the ui profile shell on the canonical <c>ui-profile-root-positive</c> vector: read the root
/// template, its shell options, the appearance and the new screens default as typed nodes, change each through the typed authoring
/// transaction and see every mistake refused with the compiler's own PLAY0790-PLAY0799 diagnostic. The tools are the ones every
/// construct uses (<c>syntax-schema</c>, <c>read-ast</c>, <c>propose-ast</c>); no rule lives in the tool.
/// </summary>
public class when_authoring_the_ui_profile_shell : given.an_authoring_connection
{
    JsonElement _staleRoot;
    JsonElement _unknownRoot;
    JsonElement _dialogRoot;
    JsonElement _shellWithoutContent;
    JsonElement _unlistedBlueprint;
    JsonElement _unknownOption;
    JsonElement _badScheme;
    JsonElement _badAccent;
    JsonElement _layoutAsDefault;
    JsonElement _secondRoot;
    string _rootChanged = string.Empty;
    string _optionAdded = string.Empty;
    string _optionRemoved = string.Empty;
    string _appearanceChanged = string.Empty;
    string _defaultChanged = string.Empty;
    string _blueprintPinChanged = string.Empty;
    string _appearanceRemoved = string.Empty;
    string _reopened = string.Empty;
    JsonElement _profiles;
    JsonElement _profilesAfter;
    JsonElement _unknownProfile;
    readonly Dictionary<string, string[]> _schemaMembers = [];

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        var empty = Open();
        var shell = ScreenCompositionCorpus.V1.TypedSourceCases.Single(sourceCase => sourceCase.Name == "ui-profile-root-positive").Document.Text;
        Apply(empty, Result("propose-source", Arguments(empty, new { operation = "create-document", path = "application.play", stableKey = "application", source = shell })));
        var created = empty.GetProperty("revision").GetString()!;

        foreach (var kind in new[] { "RootTemplateSyntax", "ShellOptionSyntax", "ProfileAppearanceSyntax", "NewScreenTemplateSyntax" })
        {
            _schemaMembers[kind] = [.. Result("syntax-schema", new { kind }).GetProperty("properties").EnumerateObject().Select(property => property.Name)];
        }

        var opened = Open();
        _profiles = Result("ui-profiles", new { });
        _staleRoot = Attempt(opened, Replace(Root(opened, "Desktop"), root => root["name"] = "Docked"), created);

        // Root template: own templates and blueprint templates.
        _unknownRoot = Attempt(opened, Replace(Root(opened, "Desktop"), root => root["name"] = "Ghost"));
        _dialogRoot = Attempt(opened, Replace(Root(opened, "Desktop"), root => root["name"] = "Sheet"));
        _unlistedBlueprint = Attempt(opened, Replace(Root(opened, "Desktop"), root => { root["name"] = "Frame"; root["blueprint"] = "Hr"; }));
        // A shell option the shell does not expose is a warning: it is accepted, and the profile read says so.
        opened = Open();
        _ = ApplyChange(new { operation = "add", parent = Root(opened, "Desktop").GetProperty("handle"), member = "options", node = Option("sparkle", "1", "Number") });
        _unknownOption = Result("ui-profiles", new { name = "Desktop" });
        opened = Open();
        _ = ApplyChange(Remove(Nodes("ShellOptionSyntax", opened).Single(node => node.GetProperty("node").GetProperty("name").GetString() == "sparkle")));

        opened = Open();
        _rootChanged = ApplyChange(Replace(Root(opened, "Desktop"), root => root["name"] = "Docked"));

        opened = Open();
        _blueprintPinChanged = ApplyChange(Replace(Root(opened, "FromBlueprint"), root => root["version"] = "~4.5.0"));

        // Shell options.
        opened = Open();
        _optionRemoved = ApplyChange(Remove(Nodes("ShellOptionSyntax", opened).Single(node => node.GetProperty("node").GetProperty("name").GetString() == "showFooter")));

        opened = Open();
        _optionAdded = ApplyChange(new { operation = "add", parent = Root(opened, "FromBlueprint").GetProperty("handle"), member = "options", node = Option("contrast", "high", "Word") });

        // Appearance.
        opened = Open();
        _badScheme = Attempt(opened, Replace(Appearance(opened, "Desktop"), appearance => appearance["colorScheme"] = "sepia"));
        _badAccent = Attempt(opened, Replace(Appearance(opened, "Desktop"), appearance => appearance["accent"] = "indigo"));
        _appearanceChanged = ApplyChange(Replace(Appearance(opened, "Desktop"), appearance =>
        {
            appearance["colorScheme"] = "light";
            appearance["accent"] = "#0EA5E9";
        }));

        opened = Open();
        _appearanceRemoved = ApplyChange(Remove(Appearance(opened, "FromBlueprint")));

        // New screens default.
        opened = Open();
        _layoutAsDefault = Attempt(opened, Replace(NewScreen(opened, "Desktop"), template => template["name"] = "AppShell"));
        _defaultChanged = ApplyChange(Replace(NewScreen(opened, "FromBlueprint"), template => template["name"] = "Detail"));

        opened = Open();
        _profilesAfter = Result("ui-profiles", new { });
        _unknownProfile = Call("ui-profiles", new { name = "Ghost" });
        _reopened = File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }

    [Fact] void should_describe_the_root_template_in_the_json_schema() => Missing("RootTemplateSyntax", "name", "blueprint", "version", "options").ShouldBeEmpty();
    [Fact] void should_describe_a_shell_option_in_the_json_schema() => Missing("ShellOptionSyntax", "name", "value", "syntaxKind").ShouldBeEmpty();
    [Fact] void should_describe_the_appearance_in_the_json_schema() => Missing("ProfileAppearanceSyntax", "colorScheme", "accent").ShouldBeEmpty();
    [Fact] void should_describe_the_new_screens_default_in_the_json_schema() => Missing("NewScreenTemplateSyntax", "name", "blueprint", "version").ShouldBeEmpty();
    [Fact] void should_refuse_a_stale_revision() => _staleRoot.GetRawText().ShouldContain("StaleRevision");
    [Fact] void should_refuse_a_root_template_nothing_declares() => _unknownRoot.GetRawText().ShouldContain("PLAY0790");
    [Fact] void should_refuse_a_screen_template_as_the_shell() => _dialogRoot.GetRawText().ShouldContain("PLAY0794");
    [Fact] void should_refuse_a_blueprint_the_profile_does_not_list() => _unlistedBlueprint.GetRawText().ShouldContain("PLAY0792");
    [Fact] void should_switch_the_root_template() => _rootChanged.ShouldContain("ui profile Desktop\n  target platform web\n\n  packages\n    Cratis.Components\n    Cratis.Blueprint.Default\n\n  root template Docked");
    [Fact] void should_change_the_version_pin_of_a_blueprint_root() => _blueprintPinChanged.ShouldContain("root template blueprint Cratis.Blueprint.Default Frame version \"~4.5.0\"");
    [Fact] void should_warn_about_an_option_the_shell_does_not_expose() => _unknownOption.GetProperty("profiles")[0].GetProperty("diagnostics").EnumerateArray().Select(diagnostic => diagnostic.GetProperty("code").GetString()).ShouldContainOnly("PLAY0796");
    [Fact] void should_read_every_profile() => _profiles.GetProperty("profiles").EnumerateArray().Select(profile => profile.GetProperty("name").GetString()).ShouldContainOnly("Desktop", "Docked", "FromBlueprint");
    [Fact] void should_read_the_root_template_with_its_option() => (_profiles.GetProperty("profiles")[0].GetProperty("root").GetProperty("name").GetString(), _profiles.GetProperty("profiles")[0].GetProperty("root").GetProperty("options")[0].GetProperty("name").GetString()).ShouldEqual(("AppShell", "showFooter"));
    [Fact] void should_read_the_appearance_and_the_new_screens_default() => (_profiles.GetProperty("profiles")[0].GetProperty("appearance").GetProperty("colorScheme").GetString(), _profiles.GetProperty("profiles")[0].GetProperty("newScreenTemplate").GetProperty("name").GetString()).ShouldEqual(("dark", "Sheet"));
    [Fact] void should_suggest_the_own_templates_that_can_be_the_shell() => _profiles.GetProperty("applicableRootTemplates").EnumerateArray().Select(template => template.GetProperty("name").GetString()).ShouldContainOnly("AppShell", "Docked");
    [Fact] void should_suggest_the_own_templates_that_can_start_a_screen() => _profiles.GetProperty("applicableNewScreenTemplates").EnumerateArray().Select(template => template.GetProperty("name").GetString()).ShouldContainOnly("Sheet");
    [Fact] void should_name_the_values_an_appearance_accepts() => _profiles.GetProperty("appearance").GetProperty("colorSchemes").EnumerateArray().Select(scheme => scheme.GetString()).ShouldContainOnly("light", "dark", "system");
    [Fact] void should_read_the_edited_shell_back() => _profilesAfter.GetProperty("profiles").EnumerateArray().Single(profile => profile.GetProperty("name").GetString() == "Desktop").GetProperty("root").GetProperty("name").GetString().ShouldEqual("Docked");
    [Fact] void should_refuse_an_unknown_profile() => _unknownProfile.GetRawText().ShouldContain("Unknown ui profile");
    [Fact] void should_remove_an_option() => _optionRemoved.ShouldNotContain("option showFooter");
    [Fact] void should_add_an_option() => _optionAdded.ShouldContain("option contrast high");
    [Fact] void should_refuse_an_unknown_color_scheme() => _badScheme.GetRawText().ShouldContain("PLAY0797");
    [Fact] void should_refuse_an_accent_that_is_not_a_hex_colour() => _badAccent.GetRawText().ShouldContain("PLAY0797");
    [Fact] void should_change_the_color_scheme() => _appearanceChanged.ShouldContain("color scheme light");
    [Fact] void should_change_the_accent() => _appearanceChanged.ShouldContain("accent \"#0EA5E9\"");
    [Fact] void should_remove_the_appearance() => _appearanceRemoved.ShouldNotContain("color scheme system");
    [Fact] void should_refuse_a_layout_as_the_new_screens_default() => _layoutAsDefault.GetRawText().ShouldContain("PLAY0799");
    [Fact] void should_change_the_new_screens_default() => _defaultChanged.ShouldContain("new screens use template blueprint Cratis.Blueprint.Default Detail");
    [Fact] void should_keep_every_applied_edit_after_reopening() => new[] { "root template Docked", "version \"~4.5.0\"", "option contrast high", "accent \"#0EA5E9\"", "Detail" }.Where(text => !_reopened.Contains(text, StringComparison.Ordinal)).ShouldBeEmpty();

    string[] Missing(string kind, params string[] members) => [.. members.Where(member => !_schemaMembers[kind].Contains(member))];

    JsonElement Root(JsonElement opened, string profile) => Member("RootTemplateSyntax", opened, profile);

    JsonElement Appearance(JsonElement opened, string profile) => Member("ProfileAppearanceSyntax", opened, profile);

    JsonElement NewScreen(JsonElement opened, string profile) => Member("NewScreenTemplateSyntax", opened, profile);

    JsonElement Member(string kind, JsonElement opened, string profile)
    {
        var profilePath = Nodes("UiProfileSyntax", opened).Single(node => node.GetProperty("node").GetProperty("name").GetString() == profile).GetProperty("handle").GetProperty("path").GetString();
        return Nodes(kind, opened).Single(node => node.GetProperty("parent").GetProperty("path").GetString() == profilePath);
    }

    static JsonObject Option(string name, string value, string kind) => new() { ["kind"] = "ShellOptionSyntax", ["name"] = name, ["value"] = value, ["syntaxKind"] = kind };

    static string Id(JsonElement node) => node.GetProperty("node").GetProperty("id").GetString()!;

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
