// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_names_a_new_screen_template_that_cannot_start_a_screen : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          topbar
          content

        module M
          screen template Docked
            category Shell
            scopes application
            content

          screen template Wide
            category Page
            scopes module
            content

          dialog template Confirm
            category Dialog
            content

        ui profile A
          new screens use template AppShell

        ui profile B
          new screens use template Docked

        ui profile C
          new screens use template Wide

        ui profile D
          new screens use template Confirm
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_each_template() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.NewScreenTemplateNotApplicable).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(21, 24, 27, 30);
    [Fact] void should_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.NewScreenTemplateNotApplicable).ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_say_why_a_scope_excludes_it() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.NewScreenTemplateNotApplicable).ElementAt(2).Message.ShouldContain("does not declare scope 'feature'");
    [Fact] void should_not_check_it_as_a_shell() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateWithoutContentOutlet).ShouldBeEmpty();
}
