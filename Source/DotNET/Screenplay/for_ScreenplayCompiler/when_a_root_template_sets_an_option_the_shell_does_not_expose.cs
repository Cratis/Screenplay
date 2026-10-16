// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_root_template_sets_an_option_the_shell_does_not_expose : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          topbar
          content

        exposure for AppShell
          property topbar.showFooter label "Footer"

        ui profile Desktop
          root template AppShell
            option showFooter true
            option sparkle yes
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_only_the_unexposed_option() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(11);
    [Fact] void should_report_a_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_option_and_what_the_shell_exposes() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownShellOption).Single().Message.ShouldContain("no option 'sparkle' - it exposes 'showFooter'");
    [Fact] void should_still_succeed() => _result.Success.ShouldBeTrue();
}
