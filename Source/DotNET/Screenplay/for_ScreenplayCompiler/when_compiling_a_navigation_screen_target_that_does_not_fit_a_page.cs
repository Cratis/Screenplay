// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_screen_target_that_does_not_fit_a_page : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(when_compiling_a_navigation_screen_target.Layout + """
        module Shop
          outlet sidebar extends
            item dialog title "Dialog" target Shop.Projects screen Dialog
            item pane title "Pane" target Shop.Projects screen Pane
            item multi title "Multi" target Shop.Projects screen Multi
            item default title "Default" target Shop.Projects screen Default
          feature Projects
            slice StateView Listing
              screen Dialog
                fits dialog
              screen Pane
                fits pane
              screen Multi
                fits page, dialog
              screen Default
        """);

    [Fact] void should_reject_non_page_targets_only() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.NonPageNavigationScreen).ShouldEqual(2);
    [Fact] void should_reject_the_document() => _result.Success.ShouldBeFalse();
}
