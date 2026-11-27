// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_sticky_note_directives : Specification
{
    const string Source =
        """
        sticky note on personas
          description "Remember"
          colour pink
          position 1 2
          size 3 4

        sticky note on seeddata
          description "Odd"
          colour purple
          position 5 6
          shape round
        """;

    CompilationResult<ApplicationSyntax> _result;
    string[] _messages;
    StickyNoteSyntax[] _notes;

    void Because()
    {
        _result = new ScreenplayCompiler().Compile(Source);
        _messages = [.. _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")];
        _notes = [.. _result.Value!.StickyNotes];
    }

    [Fact] void should_report_the_invalid_colour() => _messages.ShouldContain($"{DiagnosticCodes.InvalidPersonaDeclaration} Invalid colour 'colour purple' - expected 'colour yellow', 'colour pink', 'colour green' or 'colour blue'");
    [Fact] void should_report_the_unknown_directive() => _messages.ShouldContain($"{DiagnosticCodes.UnknownPersonaDirective} Unexpected 'shape' in sticky note body - expected description, colour, position or size");
    [Fact] void should_keep_both_notes() => _notes.Select(note => note.Canvas).ShouldEqual("personas", "seeddata");
    [Fact] void should_keep_the_text() => _notes[0].Text.ShouldEqual("Remember");
    [Fact] void should_keep_the_colour() => _notes[0].Colour.ShouldEqual("pink");
    [Fact] void should_keep_no_colour_when_it_is_invalid() => _notes[1].Colour.ShouldBeNull();
    [Fact] void should_keep_the_x_position() => _notes[0].X.ShouldEqual(1);
    [Fact] void should_keep_the_y_position() => _notes[0].Y.ShouldEqual(2);
    [Fact] void should_keep_the_width() => _notes[0].Width.ShouldEqual(3);
    [Fact] void should_keep_the_height() => _notes[0].Height.ShouldEqual(4);
}
