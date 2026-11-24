// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_malformed_navigation_screen_target : given.a_compiler
{
    [Theory]
    [InlineData("target Shop.Projects screen Projects screen Projects")]
    [InlineData("target Shop.Projects screen \"Projects\"")]
    [InlineData("target Shop.Projects screen Projects.List")]
    [InlineData("target Shop.Projects screen")]
    [InlineData("target Shop screen Projects")]
    [InlineData("screen Projects")]
    void should_reject_the_clause_even_on_a_group(string clause)
    {
        var result = _compiler.Compile(when_compiling_a_navigation_screen_target.Layout + $"""
            module Shop
              outlet sidebar extends
                item group title "Group" {clause}
                  item child title "Child" target Shop.Projects
              feature Projects
                slice StateView Listing
                  screen Projects
            """);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidNavigationItem).ShouldBeTrue();
    }
}
