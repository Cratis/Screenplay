// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_profile_with_an_incompatible_theme : given.a_compiler
{
    const string Source =
        """
        theme Midnight
          compatible with core

        ui profile Desktop
          packages
            core
            PrimeReact
            Internal.Widgets

          theme Midnight
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_warnings() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_two_diagnostics() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_report_them_all_as_the_incompatibility_code() =>
        _result.Diagnostics.All(diagnostic => diagnostic.Code == DiagnosticCodes.ThemeNotCompatibleWithPackage).ShouldBeTrue();
    [Fact] void should_name_the_incompatible_packages() =>
        _result.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldContainOnly(
            "Theme 'Midnight' is not declared compatible with package 'PrimeReact' - components from that package may not receive Midnight's styling",
            "Theme 'Midnight' is not declared compatible with package 'Internal.Widgets' - components from that package may not receive Midnight's styling");
}
