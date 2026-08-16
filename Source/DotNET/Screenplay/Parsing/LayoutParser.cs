// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the three slot bearing structures - the application's <c>layout</c>, and the <c>screen template</c>
/// and <c>dialog template</c> declarations of a module.
/// </summary>
/// <remarks>
/// They share a body - slots, and the <c>arrangement</c> arranging them, parsed by <see cref="ArrangementParser"/> -
/// and differ only in where they sit and what they say about their parent. A layout is the application's base
/// navigational look and is selected by a <c>ui profile</c>; a screen template goes inside it and says which
/// slot of its parent it fills; a dialog template opens over the application and so fills none.
/// </remarks>
internal static partial class LayoutParser
{
    /// <summary>
    /// Parses a layout from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>layout</c> header.</param>
    /// <returns>The parsed <see cref="LayoutSyntax"/>.</returns>
    public static LayoutSyntax ParseLayout(ParserContext context, SourceLine header)
    {
        var match = LayoutRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidLayoutDeclaration, $"Invalid layout declaration '{header.Content}' - expected 'layout <Name>'", header.Location);
        }

        var name = match.Success ? match.Groups[1].Value : LineText.FirstWord(header.Content["layout".Length..].Trim());
        var body = ArrangementParser.ParseBody(context, header, "layout", name, allowsFitsSlot: false);
        return new(name, body.Slots, header.Location, body.Arrangement);
    }

    /// <summary>
    /// Parses a screen template from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>screen template</c> header.</param>
    /// <returns>The parsed <see cref="ScreenTemplateSyntax"/>.</returns>
    public static ScreenTemplateSyntax ParseScreenTemplate(ParserContext context, SourceLine header)
    {
        var match = ScreenTemplateRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidScreenTemplateDeclaration, $"Invalid screen template declaration '{header.Content}' - expected 'screen template <Name>'", header.Location);
        }

        var name = match.Success ? match.Groups[1].Value : string.Empty;
        var body = ArrangementParser.ParseBody(context, header, "screen template", name, allowsFitsSlot: true);
        return new(name, body.Slots, header.Location, body.FitsSlot, body.Arrangement);
    }

    /// <summary>
    /// Parses a dialog template from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>dialog template</c> header.</param>
    /// <returns>The parsed <see cref="DialogTemplateSyntax"/>.</returns>
    public static DialogTemplateSyntax ParseDialogTemplate(ParserContext context, SourceLine header)
    {
        var match = DialogTemplateRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDialogTemplateDeclaration, $"Invalid dialog template declaration '{header.Content}' - expected 'dialog template <Name>'", header.Location);
        }

        var name = match.Success ? match.Groups[1].Value : string.Empty;
        var body = ArrangementParser.ParseBody(context, header, "dialog template", name, allowsFitsSlot: false);
        return new(name, body.Slots, header.Location, body.Arrangement);
    }

    [GeneratedRegex(@"^layout\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex LayoutRegex();

    [GeneratedRegex(@"^screen\s+template\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ScreenTemplateRegex();

    [GeneratedRegex(@"^dialog\s+template\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex DialogTemplateRegex();
}
