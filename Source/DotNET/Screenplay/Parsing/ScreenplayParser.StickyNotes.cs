// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses sticky notes for the screenplay parser.
/// </summary>
internal static partial class ScreenplayParser
{
    // 'sticky note on <canvas>' with the text it carries, an optional colour and where it sits on that canvas.
    static StickyNoteSyntax? ParseStickyNote(ParserContext context, SourceLine line)
    {
        var canvasMatch = StickyNoteRegex().Match(line.Content);
        if (!canvasMatch.Success)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid sticky note declaration '{line.Content}' - expected 'sticky note on personas', 'sticky note on eventsources', 'sticky note on givencontexts' or 'sticky note on seeddata'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var body = new StickyNoteBodyState();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            ParseStickyNoteDirective(context, child, body);
        }

        if (body.Description is not { } text || body.Position is not { } position)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, "A sticky note needs a description (the text on it) and a position", line.Location);
            return null;
        }

        return new(canvasMatch.Groups[1].Value, text, body.Colour, position.X, position.Y, body.Size?.Width, body.Size?.Height, line.Location) { DirectiveLocations = body.DirectiveLocations };
    }

    static void ParseStickyNoteDirective(ParserContext context, SourceLine child, StickyNoteBodyState body)
    {
        switch (LineText.FirstWord(child.Content))
        {
            case "description":
                body.ParseDescription(context, child, "A sticky note");
                break;
            case "colour":
                ParseStickyNoteColour(context, child, body);
                break;
            case "position":
                body.ParsePosition(context, child);
                break;
            case "size":
                body.ParseSize(context, child);
                break;
            default:
                context.Error(DiagnosticCodes.UnknownPersonaDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in sticky note body - expected description, colour, position or size", child.Location);
                context.SkipBlock(child.Indent);
                break;
        }
    }

    static void ParseStickyNoteColour(ParserContext context, SourceLine child, StickyNoteBodyState body)
    {
        if (StickyNoteColourRegex().Match(child.Content) is not { Success: true } colourMatch)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid colour '{child.Content}' - expected 'colour yellow', 'colour pink', 'colour green' or 'colour blue'", child.Location);
            return;
        }

        body.Colour = colourMatch.Groups[1].Value;
    }

    sealed class StickyNoteBodyState : CanvasBodyState
    {
        public string? Colour { get; set; }
    }
}
