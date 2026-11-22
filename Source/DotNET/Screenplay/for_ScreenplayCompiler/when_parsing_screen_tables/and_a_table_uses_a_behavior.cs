// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_tables;

public class and_a_table_uses_a_behavior : given.a_compiler
{
    ScreenTableSyntax _table;

    void Because()
    {
        var result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    table Items
                      uses Highlight
                      on row-click navigate to Details
            """);
        _table = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenTableSyntax>().Single();
    }

    [Fact] void should_attach_the_used_behavior() => _table.UsedBehaviors.Single().Behavior.ShouldEqual("Highlight");
    [Fact] void should_not_add_an_inline_behavior() => _table.Behaviors.ShouldBeEmpty();
    [Fact] void should_still_set_the_row_click() => _table.RowClick!.Screen.ShouldEqual("Details");
}
