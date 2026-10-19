// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_profile_with_an_unterminated_quoted_theme : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          theme "Scene Default Light
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_theme() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidProfileTheme);
    [Fact] void should_show_both_accepted_forms() => _result.Diagnostics.Single().Message.ShouldContain("theme \"<Name>\"");
}
