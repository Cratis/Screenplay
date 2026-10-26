// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_preserving_for_identifiers_in_screen_headers : given.a_compiler
{
    [Theory]
    [InlineData("action for", "for", null, null)]
    [InlineData("action for for screen", "for", ScreenActionSubjectKind.Screen, null)]
    [InlineData("action Work.Items.Details.for for item", "Work.Items.Details.for", ScreenActionSubjectKind.Item, null)]
    [InlineData("action label", "label", null, null)]
    [InlineData("action label for screen label \"Retry for screen\"", "label", ScreenActionSubjectKind.Screen, "Retry for screen")]
    [InlineData("action for label \"Retry for screen\"", "for", null, "Retry for screen")]
    [InlineData("action for for item label \"Retry \\\"for\\\" screen\"", "for", ScreenActionSubjectKind.Item, "Retry \"for\" screen")]
    [InlineData("action for for screen label $strings.for.label", "for", ScreenActionSubjectKind.Screen, "$strings.for.label")]
    void should_preserve_the_plain_command_and_trailing_metadata(string header, string command, ScreenActionSubjectKind? subject, string? label)
    {
        var result = Compile(header);
        var action = Screen(result).Directives.OfType<ScreenActionSyntax>().Single();
        action.Command.ShouldEqual(command);
        action.Subject.ShouldEqual(subject);
        action.Label.ShouldEqual(label);
        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("action \"Retry for screen\"", "Retry for screen", null)]
    [InlineData("action \"Retry \\\"for\\\" screen\" for item", "Retry \"for\" screen", ScreenActionSubjectKind.Item)]
    [InlineData("action $strings.for.label for screen", "$strings.for.label", ScreenActionSubjectKind.Screen)]
    void should_keep_guarded_operands_opaque(string header, string label, ScreenActionSubjectKind? subject)
    {
        var result = Compile(header + "\n  when item.ready == true execute for");
        var action = Screen(result).Directives.OfType<ScreenGuardedActionSyntax>().Single();
        action.Label.ShouldEqual(label);
        action.Subject.ShouldEqual(subject);
        action.Alternatives.Single().Command.ShouldEqual("for");
        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("item for action Retry", "for", "Retry", null)]
    [InlineData("item retry action for", "retry", "for", null)]
    [InlineData("item for action for", "for", "for", null)]
    [InlineData("item for action for for screen", "for", "for", ScreenActionSubjectKind.Screen)]
    [InlineData("item for action Retry for item", "for", "Retry", ScreenActionSubjectKind.Item)]
    [InlineData("item label action label for screen", "label", "label", ScreenActionSubjectKind.Screen)]
    [InlineData("item for action to for", "for", "for", null)]
    [InlineData("item for action to for for screen", "for", "for", ScreenActionSubjectKind.Screen)]
    [InlineData("item for action Work.Items.Details.for for item", "for", "Work.Items.Details.for", ScreenActionSubjectKind.Item)]
    void should_consume_the_complete_toolbar_action_header(string header, string name, string target, ScreenActionSubjectKind? subject)
    {
        var result = Compile("toolbar\n  " + header);
        var item = Screen(result).Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single();
        item.Name.ShouldEqual(name);
        item.Target.ShouldEqual(target);
        item.Subject.ShouldEqual(subject);
        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("item for navigate to for", "for", "for")]
    [InlineData("item for navigate for", "for", "for")]
    [InlineData("item for dialog for", "for", "for")]
    [InlineData("item for dialog to for", "for", "for")]
    [InlineData("item label navigate to label", "label", "label")]
    void should_preserve_non_action_toolbar_operands(string header, string name, string target)
    {
        var result = Compile("toolbar\n  " + header);
        var item = Screen(result).Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single();
        item.Name.ShouldEqual(name);
        item.Target.ShouldEqual(target);
        item.Subject.ShouldBeNull();
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_preserve_non_action_directive_fields()
    {
        var result = Compile("navigate to for\ndata Row via query for\ncomponent for screen\ncomponent Widget for\ntemplate for");
        var directives = Screen(result).Directives.ToArray();
        directives.OfType<ScreenNavigateSyntax>().Single().Screen.ShouldEqual("for");
        directives.OfType<ScreenDataSyntax>().Last().Query.ShouldEqual("for");
        directives.OfType<ScreenComponentSyntax>().First().Component.ShouldEqual("for");
        directives.OfType<ScreenComponentSyntax>().First().Name.ShouldEqual("screen");
        directives.OfType<ScreenComponentSyntax>().Last().Name.ShouldEqual("for");
        directives.OfType<ScreenTemplateReferenceSyntax>().Single().Name.ShouldEqual("for");
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_preserve_inputs_and_child_labels()
    {
        var result = Compile("action for for item label \"Inline for screen\"\n  with note from item.note\n  label \"Child for screen\"\ntoolbar\n  item for action for for screen label \"Toolbar for item\"");
        var action = Screen(result).Directives.OfType<ScreenActionSyntax>().Single();
        action.Subject.ShouldEqual(ScreenActionSubjectKind.Item);
        action.Label.ShouldEqual("Child for screen");
        action.Inputs.Single().Binding.ShouldEqual("item.note");
        Screen(result).Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single().Label.ShouldEqual("Toolbar for item");
        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("for")]
    [InlineData("for planet")]
    [InlineData("for item for screen")]
    [InlineData("label \"Try\" for screen")]
    void should_reject_malformed_tails_after_keyword_named_operands(string suffix)
    {
        var result = Compile("action for " + suffix + "\ntoolbar\n  item for action for " + suffix);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionSubjectClause).ShouldEqual(2);
    }

    [Theory]
    [InlineData("item for action for for screen label \"Retry for item\"", "Retry for item")]
    [InlineData("item for action for for screen label $strings.for.label", "$strings.for.label")]
    void should_preserve_toolbar_labels_and_parameters(string header, string label)
    {
        var result = Compile("toolbar\n  " + header + "\n    parameter note from data item.note");
        var item = Screen(result).Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single();
        item.Name.ShouldEqual("for");
        item.Target.ShouldEqual("for");
        item.Subject.ShouldEqual(ScreenActionSubjectKind.Screen);
        item.Label.ShouldEqual(label);
        item.Parameters.Single().Binding.Path.ShouldEqual("item.note");
        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("toolbar\n  item for navigate to for for item")]
    [InlineData("toolbar\n  item for dialog for for screen")]
    [InlineData("navigate to for for item")]
    [InlineData("data Row via query for for screen")]
    [InlineData("component for screen for item")]
    void should_reject_subject_tails_on_non_actions_after_complete_operands(string directive) =>
        Compile(directive).Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionSubjectClause).ShouldEqual(1);

    CompilationResult<ApplicationSyntax> Compile(string directives) => _compiler.Compile("""
        module Work
          feature Items
            slice StateView Details
              command for
              command Retry
              command label
              query Rows => Row[]
              query for => Row
              screen Details
                data Row[] via query Rows
        """ + "\n" + string.Join("\n", directives.Split('\n').Select(line => "        " + line)));

    static ScreenSyntax Screen(CompilationResult<ApplicationSyntax> result) => result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single();
}
