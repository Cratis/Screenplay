// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_ui_profile_with_invalid_root_template_declarations : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          root template
          root template blueprint Cratis
          root template Own version "^1"
          root template Shell
            option navigationMode slim
            option navigationMode overlay
            option broken
            option bad two words
            setting x y
          root template Other
          new screens use template
          new screens use template Sheet
          new screens use template Other
          new screens use template blueprint Bp Name version "banana"
          appearance
            color scheme dark
            color scheme light
            accent "#fff"
            accent "#000"
            tint blue
          appearance
            color scheme light
          flavour sweet
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    IEnumerable<string> Messages(string code) => _result.Diagnostics.Where(diagnostic => diagnostic.Code == code).Select(diagnostic => diagnostic.Message);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_each_malformed_or_repeated_shell_line() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count().ShouldEqual(10);
    [Fact] void should_report_malformed_options() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count(message => message.Contains("Invalid option")).ShouldEqual(2);
    [Fact] void should_report_a_repeated_option() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count(message => message.Contains("already sets option 'navigationMode'")).ShouldEqual(1);
    [Fact] void should_report_a_foreign_line_under_the_root_template() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count(message => message.Contains("Unexpected 'setting'")).ShouldEqual(1);
    [Fact] void should_report_a_version_on_an_own_template() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count(message => message.Contains("only applies to a blueprint")).ShouldEqual(1);
    [Fact] void should_report_a_repeated_new_screen_template_even_when_the_repeat_has_a_bad_range() => Messages(DiagnosticCodes.InvalidProfileRootTemplate).Count(message => message.Contains("already declares 'new screens use template'")).ShouldEqual(2);
    [Fact] void should_report_a_second_root_template() => Messages(DiagnosticCodes.DuplicateProfileRootTemplate).Count().ShouldEqual(1);
    [Fact] void should_report_a_bad_version_range() => Messages(DiagnosticCodes.InvalidTemplateVersionRange).Count().ShouldEqual(1);
    [Fact] void should_report_each_malformed_or_repeated_appearance_line() => Messages(DiagnosticCodes.InvalidProfileAppearance).Count().ShouldEqual(4);
    [Fact] void should_report_an_unknown_profile_directive() => Messages(DiagnosticCodes.UnknownUiProfileDirective).Count().ShouldEqual(1);
    [Fact] void should_keep_the_first_root_template() => _result.Value!.UiProfiles!.Single().Root!.Name.ShouldEqual("Shell");
    [Fact] void should_keep_the_first_option_value() => _result.Value!.UiProfiles!.Single().Root!.Options.Single().Value.ShouldEqual("slim");
    [Fact] void should_keep_the_first_appearance_block() => _result.Value!.UiProfiles!.Single().Appearance!.ColorScheme.ShouldEqual("dark");
}
