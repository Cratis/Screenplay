// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_components;

public class and_a_component_has_an_unknown_child : given.a_compiler
{
    Diagnostic[] _unknown;
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
                      sparkle
                        icon skipped
                      icon star
            """);
        _component = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single();
        _unknown = [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreenDirective)];
    }

    [Fact] void should_report_one_unknown_directive() => _unknown.Length.ShouldEqual(1);
    [Fact] void should_report_the_exact_message() => _unknown[0].Message.ShouldEqual("Unexpected 'sparkle' in component - expected context, id, property, icon, presentation, exposes, slot, on or uses");
    [Fact] void should_skip_the_unknown_childs_block() => _component.Icon.ShouldEqual("star");
}
