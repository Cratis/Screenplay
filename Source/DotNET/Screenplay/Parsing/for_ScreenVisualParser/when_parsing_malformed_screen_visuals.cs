// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_ScreenVisualParser;

public class when_parsing_malformed_screen_visuals : Specification
{
    [Theory]
    [InlineData("sketch")]
    [InlineData("mockup")]
    [InlineData("image")]
    public void should_refuse_a_visual_without_a_path(string line) =>
        Parse(line).ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidScreenVisual && diagnostic.Severity == DiagnosticSeverity.Error);

    [Theory]
    [InlineData("image Screens/books.pdf")]
    [InlineData("mockup Screens/books.png")]
    public void should_warn_about_a_file_that_does_not_fit_the_kind(string line) =>
        Parse(line).ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnexpectedScreenVisualFile && diagnostic.Severity == DiagnosticSeverity.Warning);

    [Fact] void should_warn_about_an_absolute_path() => Parse("image /home/me/books.png").ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.AbsoluteFileReference);

    static IEnumerable<Diagnostic> Parse(string line) => new ScreenplayCompiler().Parse($"""
        module Lending
          feature Books
            slice StateView ListBooks
              screen Books
                {line}
        """).Diagnostics;
}
