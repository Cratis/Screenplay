// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_screen_targets_in_shell_scopes : given.a_compiler
{
    const string Source = """
        layout AppShell
          category Shell
          content page routed primary
          outlet sidebar of navigation
            default group title "Group" target Shop.Projects screen Projects
              item nested title "Nested" target Shop.Projects screen Projects
        module Shop
          feature Projects
            contribute to sidebar
              item contributed title "Contributed" target Shop.Projects screen Projects
            slice StateView Listing
              screen Projects
                fits page
                outlet sidebar extends
                  item local title "Local" target Shop.Projects screen Projects
        """;

    [Fact] void should_accept_layout_defaults_nested_items_contributions_and_screen_outlets() => _compiler.Compile(Source).Success.ShouldBeTrue();
    [Fact] void should_validate_each_scope() => _compiler.Compile(Source.Replace("screen Projects", "screen Missing", StringComparison.Ordinal).Replace("      screen Missing\n", "      screen Projects\n", StringComparison.Ordinal)).Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownNavigationScreen).ShouldEqual(4);
}
