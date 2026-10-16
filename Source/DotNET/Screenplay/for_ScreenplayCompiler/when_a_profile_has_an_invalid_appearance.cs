// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_has_an_invalid_appearance : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          appearance
            color scheme sepia
            accent "teal"

        ui profile Short
          appearance
            color scheme system
            accent "#abc"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_scheme_and_the_accent() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAppearanceValue).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(3, 4);
    [Fact] void should_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAppearanceValue).ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_name_the_scheme() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAppearanceValue).First().Message.ShouldContain("'sepia'");
    [Fact] void should_name_the_accent() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAppearanceValue).Last().Message.ShouldContain("'teal'");
}
