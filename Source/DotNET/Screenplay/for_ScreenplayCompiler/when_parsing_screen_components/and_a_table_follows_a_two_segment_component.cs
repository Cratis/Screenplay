// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_components;

public class and_a_table_follows_a_two_segment_component : given.a_compiler
{
    Diagnostic[] _triggers;

    void Because()
    {
        var result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    component App.Card card
                      on ping
                        navigate to Details
                    table Items
                      on ping
                        navigate to Details
            """);
        _triggers = [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidInteractionTrigger)];
    }

    [Fact] void should_reject_the_trigger_on_the_table() => _triggers.Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(9);
}
