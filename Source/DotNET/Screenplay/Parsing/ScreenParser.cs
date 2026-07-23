// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>screen</c> declarations - intent level directives, structural layouts and inline code.
/// </summary>
internal static partial class ScreenParser
{
    /// <summary>
    /// Parses a screen from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>screen</c> header.</param>
    /// <returns>The parsed <see cref="ScreenSyntax"/>.</returns>
    public static ScreenSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error($"Invalid screen declaration '{header.Content}' - expected 'screen <Name>'", header.Location);
        }

        FileReferenceSyntax? file = null;
        var directives = new List<ScreenDirectiveSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "file")
            {
                file = new(line.Content["file".Length..].Trim(), line.Location);
            }
            else if (ParseDirective(context, line) is { } directive)
            {
                directives.Add(directive);
            }
        }

        return new(name.Groups[1].Value, file, directives, header.Location);
    }

    static ScreenDirectiveSyntax? ParseDirective(ParserContext context, SourceLine line)
    {
        switch (LineText.FirstWord(line.Content))
        {
            case "data":
                return ParseData(context, line);
            case "action":
                return ParseAction(context, line);
            case "layout":
                return ParseLayout(context, line);
            case "section":
                return ParseSection(context, line);
            case "title":
                return ParseTitle(context, line);
            case "table":
                return ParseTable(context, line);
            case "summary":
                return ParseSummary(context, line);
            case "navigate":
                return ParseNavigate(context, line.Content, line);
            default:
                if (CodeBlockParser.Languages.Contains(line.Content))
                {
                    var code = CodeBlockParser.Parse(context, line.Content, line);
                    return code is null ? null : new ScreenCodeSyntax(code, line.Location);
                }

                context.Error($"Unexpected '{line.Content}' in screen body", line.Location);
                context.SkipBlock(line.Indent);
                return null;
        }
    }

    static ScreenDataSyntax? ParseData(ParserContext context, SourceLine line)
    {
        var match = DataRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid data directive '{line.Content}' - expected 'data <ReadModel> via query <Query> [by <param>]'", line.Location);
            return null;
        }

        var type = PropertyLineParser.ParseTypeRef(match.Groups[1].Value, line.Location);
        return new(type, match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : null, line.Location);
    }

    static ScreenActionSyntax? ParseAction(ParserContext context, SourceLine line)
    {
        var match = ActionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid action directive '{line.Content}' - expected 'action <Command>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        string? label = null;
        ScreenNavigateSyntax? navigate = null;

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var labelMatch = LabelRegex().Match(child.Content);
            if (labelMatch.Success)
            {
                label = OperandText(labelMatch, 1);
            }
            else if (LineText.FirstWord(child.Content) == "navigate")
            {
                navigate = ParseNavigate(context, child.Content, child);
            }
            else
            {
                context.Error($"Unexpected '{child.Content}' in action - expected 'label \"...\"' or 'navigate to ...'", child.Location);
            }
        }

        return new(match.Groups[1].Value, label, navigate, line.Location);
    }

    static ScreenNavigateSyntax? ParseNavigate(ParserContext context, string text, SourceLine line)
    {
        var match = NavigateRegex().Match(text);
        if (!match.Success)
        {
            context.Error($"Invalid navigation '{text}' - expected 'navigate to <Screen> [by <param>]'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location);
    }

    static ScreenLayoutSyntax ParseLayout(ParserContext context, SourceLine line)
    {
        var name = line.Content["layout".Length..].Trim();
        var slots = new List<ScreenSlotSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!SlotRegex().IsMatch(child.Content))
            {
                context.Error($"Expected a slot name in layout '{name}', got '{child.Content}'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            var directives = new List<ScreenDirectiveSyntax>();
            while (context.TryPeekChild(child.Indent, out var slotChild))
            {
                context.Reader.TakeSignificant();
                if (ParseDirective(context, slotChild) is { } directive)
                {
                    directives.Add(directive);
                }
            }

            slots.Add(new(child.Content, directives, child.Location));
        }

        return new(name, slots, line.Location);
    }

    static ScreenSectionSyntax ParseSection(ParserContext context, SourceLine line)
    {
        var name = line.Content["section".Length..].Trim();
        var directives = new List<ScreenDirectiveSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseDirective(context, child) is { } directive)
            {
                directives.Add(directive);
            }
        }

        return new ScreenSectionSyntax(name, directives, line.Location);
    }

    static ScreenTitleSyntax ParseTitle(ParserContext context, SourceLine line)
    {
        var match = TitleRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid title directive '{line.Content}' - expected 'title \"...\"'", line.Location);
            return new(string.Empty, line.Location);
        }

        return new(OperandText(match, 1), line.Location);
    }

    static ScreenTableSyntax ParseTable(ParserContext context, SourceLine line)
    {
        var target = line.Content["table".Length..].Trim();
        var columns = new List<ScreenColumnSyntax>();
        ScreenNavigateSyntax? rowClick = null;

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var column = ColumnRegex().Match(child.Content);
            if (column.Success)
            {
                columns.Add(new(column.Groups[1].Value, column.Groups[2].Success || column.Groups[3].Success ? OperandText(column, 2) : null, child.Location));
                continue;
            }

            var click = RowClickRegex().Match(child.Content);
            if (click.Success)
            {
                rowClick = ParseNavigate(context, click.Groups[1].Value, child);
                continue;
            }

            context.Error($"Unexpected '{child.Content}' in table - expected 'column ...' or 'on row-click navigate to ...'", child.Location);
        }

        return new(target, columns, rowClick, line.Location);
    }

    static ScreenSummarySyntax ParseSummary(ParserContext context, SourceLine line)
    {
        var target = line.Content["summary".Length..].Trim();
        var fields = new List<ScreenFieldSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var field = FieldRegex().Match(child.Content);
            if (!field.Success)
            {
                context.Error($"Unexpected '{child.Content}' in summary - expected 'field <property> label \"...\"'", child.Location);
                continue;
            }

            fields.Add(new(field.Groups[1].Value, OperandText(field, 2), child.Location));
        }

        return new(target, fields, line.Location);
    }

    static string OperandText(Match match, int quotedGroup) =>
        match.Groups[quotedGroup].Success ? match.Groups[quotedGroup].Value : match.Groups[quotedGroup + 1].Value;

    [GeneratedRegex(@"^screen\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^data\s+([\w.]+(?:\[\])?)\s+via\s+query\s+(\w+)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex DataRegex();

    [GeneratedRegex(@"^action\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ActionRegex();

    [GeneratedRegex("^label\\s+(?:\"([^\"]*)\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^navigate\s+to\s+(\w+)(?:\s+by\s+(\w+))?$", RegexOptions.None, 1000)]
    private static partial Regex NavigateRegex();

    [GeneratedRegex(@"^[a-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex SlotRegex();

    [GeneratedRegex("^title\\s+(?:\"([^\"]*)\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("^column\\s+([\\w.]+)(?:\\s+label\\s+(?:\"([^\"]*)\"|(\\$strings\\.\\w+(?:\\.\\w+)*)))?$", RegexOptions.None, 1000)]
    private static partial Regex ColumnRegex();

    [GeneratedRegex(@"^on\s+row-click\s+(navigate\s+to\s+.+)$", RegexOptions.None, 1000)]
    private static partial Regex RowClickRegex();

    [GeneratedRegex("^field\\s+([\\w.]+)\\s+label\\s+(?:\"([^\"]*)\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex FieldRegex();
}
