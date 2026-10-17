// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_distinct_policy_names : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        policy Access
          require authenticated
        policy access
          require authenticated
        policy Other
          require authenticated
        """);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
