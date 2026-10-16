// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_profile_with_an_unknown_layout : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          layout Ghost
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_layout() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnknownLayout);
    [Fact] void should_report_the_legacy_layout_form() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.LegacyProfileLayout);
    [Fact] void should_report_only_warnings() => _result.Diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
}
