// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_profile_with_a_quoted_theme : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          theme "Scene Default Light"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_carry_the_exact_name() => _result.Value!.UiProfiles!.Single().Theme.ShouldEqual("Scene Default Light");
    [Fact] void should_not_report_an_unknown_theme_for_a_package_provided_name() => _result.Diagnostics.ShouldBeEmpty();
}
