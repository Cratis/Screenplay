// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenVisualParser;

public class when_parsing_screen_visuals : Specification
{
    const string Source = """
        module Lending
          feature Books
            slice StateView ListBooks
              screen Books
                sketch Screens/books.sketch.json
                mockup Screens/books/index.html
                mockup Screens/books/details.html
                image Screens/books.png
        """;

    CompilationResult<ApplicationSyntax> _parsed = null!;
    ScreenSyntax _screen = null!;

    void Because()
    {
        _parsed = new ScreenplayCompiler().Parse(Source);
        _screen = _parsed.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single();
    }

    [Fact] void should_read_every_visual_in_order() => _screen.Visuals.Select(visual => visual.Kind).ShouldContainOnly(ScreenVisualKind.Sketch, ScreenVisualKind.Mockup, ScreenVisualKind.Mockup, ScreenVisualKind.Image);
    [Fact] void should_keep_the_paths() => _screen.Visuals.Select(visual => visual.File.Path).ShouldContainOnly("Screens/books.sketch.json", "Screens/books/index.html", "Screens/books/details.html", "Screens/books.png");
    [Fact] void should_not_read_a_visual_as_a_screen_directive() => _screen.Directives.ShouldBeEmpty();
    [Fact] void should_report_no_visual_diagnostics() => _parsed.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidScreenVisual || diagnostic.Code == DiagnosticCodes.UnexpectedScreenVisualFile);
}
