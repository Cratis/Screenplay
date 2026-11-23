// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_components;

public class and_a_one_segment_component_handles_a_lowercase_trigger : given.a_compiler
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
                    component Card card
                      on ping
                        navigate to Details
            """);
        _triggers = [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidInteractionTrigger)];
    }

    [Fact] void should_reject_the_trigger() => _triggers.Length.ShouldEqual(1);
}
