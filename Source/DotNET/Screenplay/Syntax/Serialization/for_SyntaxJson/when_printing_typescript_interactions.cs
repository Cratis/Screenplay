// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_printing_typescript_interactions : Specification
{
    static readonly IReadOnlyDictionary<string, string[]> RequiredMembers = new Dictionary<string, string[]>
    {
        ["ApplicationSyntax"] = ["behaviors", "layouts"],
        ["ModuleSyntax"] = ["screenTemplates", "dialogTemplates"],
        ["BehaviorSyntax"] = ["name", "parameters", "bindings", "description", "order", "file"],
        ["BehaviorParameterSyntax"] = ["name", "type"],
        // Empty alternatives and null otherwise are canonically omitted by SyntaxJsonWriter.
        // The fixture's non-default alternatives/fallback are required explicitly below.
        ["InteractionBindingSyntax"] = ["trigger", "condition", "actions"],
        ["InteractionAlternativeSyntax"] = ["condition", "actions"],
        ["InteractionOtherwiseSyntax"] = ["actions"],
        ["UsesBehaviorSyntax"] = ["behavior", "arguments"],
        ["BehaviorArgumentSyntax"] = ["name", "value"],
        ["InteractionArgumentSyntax"] = ["name", "binding"],
        ["ScreenBehaviorSyntax"] = ["behavior"],
        ["ScreenUsesBehaviorSyntax"] = ["uses"],
        ["ScreenTableSyntax"] = ["behaviors", "usedBehaviors"],
        ["ScreenComponentSyntax"] = ["behaviors", "usedBehaviors"],
        ["LayoutSyntax"] = ["behaviors", "usedBehaviors", "slots"],
        ["ScreenTemplateSyntax"] = ["behaviors", "usedBehaviors", "slots", "fitsSlot"],
        ["DialogTemplateSyntax"] = ["behaviors", "usedBehaviors", "slots"],
    };

    JsonElement _wire;
    ApplicationSyntax _typescript;
    ApplicationSyntax _native;
    ApplicationSyntax _reparsed;
    string _printed;

    void Establish()
    {
        var folder = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance");
        using var vector = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "interactions.syntax.json")));
        _wire = vector.RootElement.Clone();
        _typescript = (ApplicationSyntax)SyntaxJson.Deserialize(_wire);
        _native = new ScreenplayCompiler().Parse(File.ReadAllText(Path.Combine(folder, "interactions.play"))).Value!;
    }

    void Because()
    {
        _printed = new ScreenplayPrinter().Print(_typescript);
        _reparsed = new ScreenplayCompiler().Parse(_printed).Value!;
    }

    [Fact] void should_not_omit_required_interaction_members_from_the_typescript_vector() => MissingMembers(_wire).ShouldBeEmpty();
    [Fact] void should_require_the_nonempty_alternatives_from_the_typescript_vector() => ConditionalBinding.GetProperty("alternatives").GetArrayLength().ShouldEqual(2);
    [Fact] void should_require_the_nonempty_fallback_from_the_typescript_vector() => ConditionalBinding.GetProperty("otherwise").GetProperty("actions").GetArrayLength().ShouldEqual(2);
    [Fact] void should_restore_omitted_empty_alternatives() => _typescript.Behaviors.Single().Bindings.First().Alternatives.ShouldBeEmpty();
    [Fact] void should_restore_omitted_null_fallback() => _typescript.Behaviors.Single().Bindings.First().Otherwise.ShouldBeNull();
    [Fact] void should_restore_the_same_tree_as_the_native_parser() => SyntaxJson.StructurallyEqual(_typescript, _native).ShouldBeTrue();
    [Fact] void should_preserve_the_tree_after_printing_typescript_originated_json() => SyntaxJson.StructurallyEqual(_typescript, _reparsed).ShouldBeTrue();
    [Fact] void should_print_stably_on_a_second_pass() => new ScreenplayPrinter().Print(_reparsed).ShouldEqual(_printed);
    [Fact] void should_retain_the_named_behavior() => _reparsed.Behaviors.Single().Name.ShouldEqual("ConfirmChanges");
    [Fact] void should_retain_the_layout_attachments() => _reparsed.Layouts!.Single().UsedBehaviors.Single().Arguments.Count().ShouldEqual(2);
    [Fact] void should_retain_the_screen_template() => _reparsed.Modules.Single().ScreenTemplates.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_retain_the_dialog_template() => _reparsed.Modules.Single().DialogTemplates!.Single().UsedBehaviors.Count().ShouldEqual(1);
    [Fact] void should_retain_the_table_attachment() => Screen.Directives.OfType<ScreenTableSyntax>().Single().Behaviors.Single().Bindings.Single().Condition.ShouldEqual("item.enabled");
    [Fact] void should_retain_the_component_attachment() => Screen.Directives.OfType<ScreenComponentSyntax>().Single().UsedBehaviors.Single().Behavior.ShouldEqual("ConfirmChanges");
    [Fact] void should_keep_action_bindings_distinct_from_behavior_values() => _reparsed.Behaviors.Single().Bindings.First().Actions.OfType<ConfirmActionSyntax>().Single().OnSuccess.Single().Arguments.Single().Binding.ShouldEqual("item.id");

    JsonElement ConditionalBinding => _wire.GetProperty("behaviors")[0].GetProperty("bindings")[1];

    ScreenSyntax Screen => _reparsed.Modules.Single().Features.Single().Slices.Single().Screens.Single();

    static IEnumerable<string> MissingMembers(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Array) return node.EnumerateArray().SelectMany(MissingMembers);
        if (node.ValueKind != JsonValueKind.Object) return [];
        IEnumerable<string> own = node.TryGetProperty("kind", out var kind) && RequiredMembers.TryGetValue(kind.GetString() ?? string.Empty, out var members)
            ? members.Where(member => !node.TryGetProperty(member, out _)).Select(member => $"{kind.GetString()}.{member}")
            : [];
        return own.Concat(node.EnumerateObject().SelectMany(property => MissingMembers(property.Value)));
    }

    static string Root([CallerFilePath] string file = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(file)!);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Source", "Screenplay", "Compiler", "Conformance"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Cannot locate the TypeScript interaction vector");
    }
}
