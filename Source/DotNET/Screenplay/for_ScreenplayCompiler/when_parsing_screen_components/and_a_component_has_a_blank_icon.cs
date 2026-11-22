// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_components;

public class and_a_component_has_a_blank_icon : given.a_compiler
{
    ScreenComponentSyntax _component;

    void Because()
    {
        var result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    component App.Card card
                      icon
            """);
        _component = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single();
    }

    [Fact] void should_have_no_icon() => _component.Icon.ShouldBeNull();
}
