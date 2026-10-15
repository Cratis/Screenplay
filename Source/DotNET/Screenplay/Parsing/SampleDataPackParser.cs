// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a top level <c>sample data &lt;Name&gt;</c> block: its title, locale and the shapes with their rows.
/// </summary>
internal static partial class SampleDataPackParser
{
    /// <summary>
    /// Parses a sample data pack whose header line has just been consumed.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="header">The <c>sample data</c> header line.</param>
    /// <returns>The parsed <see cref="SampleDataPackSyntax"/>.</returns>
    public static SampleDataPackSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSampleDataDeclaration, $"Invalid sample data declaration '{header.Content}' - expected 'sample data <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), null, null, [], header.Location);
        }

        string? title = null;
        string? locale = null;
        var sets = new List<SampleDataSetSyntax>();
        var seenShapes = new HashSet<string>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "title":
                    title = ParseTitle(context, line, title);
                    break;
                case "locale":
                    locale = ParseLocale(context, line, locale);
                    break;
                case "shape":
                    ParseShape(context, line, sets, seenShapes);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownSampleDataDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in a sample data block - expected title, locale or shape", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, title, locale, sets, header.Location);
    }

    static string? ParseTitle(ParserContext context, SourceLine line, string? existing)
    {
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.UnknownSampleDataDirective, "This sample data block already declares a title - it can have at most one", line.Location);
            return existing;
        }

        var match = TitleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownSampleDataDirective, $"Invalid title '{line.Content}' - expected 'title \"...\"'", line.Location);
            return null;
        }

        return StringLiteral.Unescape(match.Groups[1].Value);
    }

    static string? ParseLocale(ParserContext context, SourceLine line, string? existing)
    {
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.UnknownSampleDataDirective, "This sample data block already declares a locale - it can have at most one", line.Location);
            return existing;
        }

        var match = LocaleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSampleDataLocale, $"Invalid locale '{line.Content}' - expected 'locale <BCP 47 tag>', for example 'locale nb-NO'", line.Location);
            return null;
        }

        return match.Groups[1].Value;
    }

    static void ParseShape(ParserContext context, SourceLine line, List<SampleDataSetSyntax> sets, HashSet<string> seenShapes)
    {
        var match = ShapeRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownSampleDataDirective, $"Invalid shape '{line.Content}' - expected 'shape <Name>'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var shape = match.Groups[1].Value;
        var rows = new List<SampleDataRowSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content != "row")
            {
                context.Error(DiagnosticCodes.UnknownSampleDataDirective, $"Unexpected '{child.Content}' in a sample data shape - expected 'row'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            rows.Add(ParseRow(context, child));
        }

        if (!seenShapes.Add(shape))
        {
            context.Error(DiagnosticCodes.DuplicateSampleDataShape, $"This sample data pack already declares shape '{shape}'", line.Location);
            return;
        }

        sets.Add(new(shape, rows, line.Location));
    }

    static SampleDataRowSyntax ParseRow(ParserContext context, SourceLine row)
    {
        var fields = new List<SampleDataFieldSyntax>();
        var seen = new HashSet<string>();
        while (context.TryPeekChild(row.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var match = FieldRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidSampleDataValue, $"Invalid row field '{line.Content}' - expected '<field> = <literal>'", line.Location);
                continue;
            }

            var name = match.Groups[1].Value;
            var field = ParseValue(context, name, match.Groups[2].Value.Trim(), line);
            if (field is null)
            {
                continue;
            }

            if (!seen.Add(name))
            {
                context.Error(DiagnosticCodes.DuplicateSampleDataField, $"This row already sets field '{name}'", line.Location);
                continue;
            }

            fields.Add(field);
        }

        return new(fields, row.Location);
    }

    static SampleDataFieldSyntax? ParseValue(ParserContext context, string name, string text, SourceLine line)
    {
        if (text == "null")
        {
            return new(name, SampleDataValueKind.Null, null, line.Location);
        }

        if (text == "true" || text == "false")
        {
            return new(name, SampleDataValueKind.Flag, text, line.Location);
        }

        if (NumberRegex().IsMatch(text))
        {
            return new(name, SampleDataValueKind.Number, text, line.Location);
        }

        var quoted = QuotedRegex().Match(text);
        if (quoted.Success)
        {
            return new(name, SampleDataValueKind.Text, StringLiteral.Unescape(quoted.Groups[1].Value), line.Location);
        }

        context.Error(DiagnosticCodes.InvalidSampleDataValue, $"Invalid value '{text}' for field '{name}' - expected a string, number, true, false or null", line.Location);
        return null;
    }

    [GeneratedRegex(@"^sample\s+data\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex("^title\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"^locale\s+([A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*)$", RegexOptions.NonBacktracking)]
    private static partial Regex LocaleRegex();

    [GeneratedRegex(@"^shape\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ShapeRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s*=(?:\s+(?<2>.+)|(?<2>[^=>\n].*))$", RegexOptions.NonBacktracking)]
    private static partial Regex FieldRegex();

    [GeneratedRegex(@"^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$", RegexOptions.NonBacktracking)]
    private static partial Regex NumberRegex();

    [GeneratedRegex("^\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex QuotedRegex();
}
