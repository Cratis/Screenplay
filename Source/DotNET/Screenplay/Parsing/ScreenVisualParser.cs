// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>sketch</c>, <c>mockup</c> and <c>image</c> lines of a screen - the visuals it carries beside the model.
/// </summary>
internal static class ScreenVisualParser
{
    static readonly string[] _imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"];
    static readonly string[] _mockupExtensions = [".html", ".htm"];

    /// <summary>
    /// Gets the keyword a <see cref="ScreenVisualKind"/> is written with.
    /// </summary>
    /// <param name="kind">The <see cref="ScreenVisualKind"/>.</param>
    /// <returns>The keyword.</returns>
    public static string Keyword(ScreenVisualKind kind) => kind switch
    {
        ScreenVisualKind.Mockup => "mockup",
        ScreenVisualKind.Image => "image",
        _ => "sketch"
    };

    /// <summary>
    /// Parses the line when it is a visual, adding it to the screen's visuals.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/>.</param>
    /// <param name="visuals">The visuals read so far.</param>
    /// <returns>True when the line was a visual line, false when it is something else.</returns>
    public static bool TryParse(ParserContext context, SourceLine line, List<ScreenVisualSyntax> visuals)
    {
        if (KindOf(LineText.FirstWord(line.Content)) is not { } kind) return false;

        var keyword = Keyword(kind);
        var path = line.Content[keyword.Length..].Trim();
        if (path.Length == 0)
        {
            context.Error(DiagnosticCodes.InvalidScreenVisual, $"'{keyword}' needs the path of the file it shows - write '{keyword} <path>', relative to the model root", line.Location);
            return true;
        }

        FileReferenceParser.WarnWhenAbsolute(context, path, line.Location);
        WarnWhenUnexpected(context, kind, path, line.Location);
        visuals.Add(new(kind, new FileReferenceSyntax(path, line.Location), line.Location));
        return true;
    }

    static ScreenVisualKind? KindOf(string word) => word switch
    {
        "sketch" => ScreenVisualKind.Sketch,
        "mockup" => ScreenVisualKind.Mockup,
        "image" => ScreenVisualKind.Image,
        _ => null
    };

    static void WarnWhenUnexpected(ParserContext context, ScreenVisualKind kind, string path, SourceLocation location)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        switch (kind)
        {
            case ScreenVisualKind.Image when !_imageExtensions.Contains(extension):
                context.Warning(DiagnosticCodes.UnexpectedScreenVisualFile, $"Image '{path}' is not a PNG, JPG, GIF, SVG or WebP file", location);
                break;
            case ScreenVisualKind.Mockup when !_mockupExtensions.Contains(extension):
                context.Warning(DiagnosticCodes.UnexpectedScreenVisualFile, $"Mockup '{path}' is not an HTML page - a mockup page ends in .html or .htm", location);
                break;
        }
    }
}
