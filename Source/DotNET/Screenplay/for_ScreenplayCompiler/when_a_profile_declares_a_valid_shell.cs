// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_declares_a_valid_shell : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          category Shell
          topbar
          content

        exposure for AppShell
          property topbar.showFooter label "Footer"

        module M
          screen template Docked
            category Shell
            scopes application
            content

          screen template Sheet
            category Page
            scopes feature
            content

        ui profile Desktop
          packages
            Cratis.Components
            Cratis.Blueprint.Default

          root template AppShell
            option showFooter true

          appearance
            color scheme dark
            accent "#4F46E5"

          new screens use template Sheet

        ui profile Docked
          root template Docked

        ui profile FromBlueprint
          packages
            Cratis.Blueprint.Default

          root template blueprint Cratis.Blueprint.Default Frame version "^4.5"
            option anything 1

          appearance
            color scheme system
            accent "#4F46E5CC"

          new screens use template blueprint Cratis.Blueprint.Default Sheet
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
}
