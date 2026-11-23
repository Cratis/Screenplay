// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_ambiguous_navigation_screen_target : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(when_compiling_a_navigation_screen_target.Layout + """
        module Shop
          outlet sidebar extends
            item projects title "Projects" target Shop.Projects screen Projects
          feature Projects
            slice StateView First
              screen Projects
            slice StateView Second
              screen Projects
        """);

    [Fact] void should_reject_the_document() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_competing_executable_screens() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousNavigationScreen).ShouldEqual(1);
}
