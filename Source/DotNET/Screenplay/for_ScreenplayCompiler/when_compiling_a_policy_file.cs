// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_policy_file : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _relative;
    CompilationResult<ApplicationSyntax> _absolute;
    CompilationResult<ApplicationSyntax> _both;

    void Because()
    {
        _relative = _compiler.Compile("policy Access\n  file Policies/Access.cs");
        _absolute = _compiler.Compile("policy Access\n  file /Users/someone/Policies/Access.cs");
        _both = _compiler.Compile("""
            policy Access
              file Policies/Access.cs
              ```csharp
              return true;
              ```
            """);
    }

    [Fact] void should_accept_a_relative_file() => _relative.Success.ShouldBeTrue();
    [Fact] void should_keep_the_relative_path() => _relative.Value!.Policies.Single().File!.Path.ShouldEqual("Policies/Access.cs");
    [Fact] void should_warn_on_an_absolute_path() => _absolute.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AbsoluteFileReference);
    [Fact] void should_still_compile_an_absolute_path() => _absolute.Success.ShouldBeTrue();
    [Fact] void should_reject_two_implementations() => _both.Success.ShouldBeFalse();
    [Fact] void should_explain_the_alternatives() => _both.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("not both", StringComparison.Ordinal)).ShouldBeTrue();
}
