// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_named_guarded_bindings;

public class and_a_behavior_is_unused : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        behavior Pick
          on click
            when item.status == "open"
              notify info "Open"
            when item.status == "open"
              notify info "Still open"
        """);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_check_shadowing_without_inventing_a_subject() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.UnreachableActionAlternative);
    [Fact] void should_locate_the_warning_at_the_shadowed_branch() => _result.Diagnostics.Single().Location.Line.ShouldEqual(5);
}
