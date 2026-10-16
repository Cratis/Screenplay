// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_names_an_unknown_root_template : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          topbar
          content

        ui profile Desktop
          root template Ghost

          new screens use template Phantom
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_root_template() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(8, 10);
    [Fact] void should_report_an_error() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_name_the_template() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).First().Message.ShouldContain("'Ghost'");
    [Fact] void should_carry_the_template_argument() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).First().Arguments["template"].ToString().ShouldContain("Ghost");
    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
}
