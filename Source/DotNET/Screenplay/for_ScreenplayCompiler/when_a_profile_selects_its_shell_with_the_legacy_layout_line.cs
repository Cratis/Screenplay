// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_selects_its_shell_with_the_legacy_layout_line : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          topbar
          content

        ui profile Desktop
          layout AppShell

        ui profile Both
          layout AppShell
          root template AppShell
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_warn_for_each_profile() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyProfileLayout).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(6, 9);
    [Fact] void should_report_a_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyProfileLayout).ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    [Fact] void should_suggest_the_canonical_form() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyProfileLayout).First().Message.ShouldContain("write 'root template AppShell'");
    [Fact] void should_say_the_layout_is_ignored_beside_a_root_template() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyProfileLayout).Last().Message.ShouldContain("is ignored");
    [Fact] void should_still_succeed() => _result.Success.ShouldBeTrue();
}
