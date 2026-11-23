// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_components;

public class and_an_invalid_id_follows_a_valid_one : given.a_compiler
{
    ScreenComponentSyntax _component;
    Diagnostic[] _unknown;

    void Because()
    {
        var result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    component App.Card card
                      id "card-1"
                      id
            """);
        _component = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single();
        _unknown = [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreenDirective)];
    }

    [Fact] void should_reset_the_stable_id() => _component.StableId.ShouldBeNull();
    [Fact] void should_report_the_invalid_id() => _unknown.Single().Message.ShouldEqual("Invalid component id 'id' - expected 'id \"stable-id\"'");
}
