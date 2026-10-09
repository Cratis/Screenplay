// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_component_interactions;

public class and_the_bindings_are_guarded : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        behavior Pick
          parameter message
          on click
            when item.status == "open"
              notify info message
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                status String
              query ItemDetails => Item
              screen Details
                data Item via query ItemDetails
                component App.Card card
                  on click
                    when item.status == "open"
                      execute MissingCommand
                  uses Pick
                    unexpected "Hello"
        """);

    [Fact] void should_check_only_the_guarded_bindings() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.UnknownActionCommand, DiagnosticCodes.UnknownBehaviorArgument, DiagnosticCodes.MissingBehaviorArgument);
    [Fact] void should_report_each_problem_only_once() => _result.Diagnostics.Count().ShouldEqual(3);
}
