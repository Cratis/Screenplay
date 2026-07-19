// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>capture</c> declarations - the change data capture sub-language.
/// </summary>
internal static partial class CaptureParser
{
    /// <summary>
    /// Parses a capture from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>capture</c> header.</param>
    /// <returns>The parsed <see cref="CaptureSyntax"/>.</returns>
    public static CaptureSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error($"Invalid capture declaration '{header.Content}' - expected 'capture <Name>'", header.Location);
        }

        CaptureSourceSyntax? source = null;
        string? key = null;
        var map = new List<CaptureMapSyntax>();
        var appends = new List<CaptureAppendSyntax>();
        var children = new List<CaptureChildrenSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "source":
                    source = ParseSource(context, line);
                    break;
                case "key":
                    key = line.Content["key".Length..].Trim();
                    break;
                case "map":
                    map.AddRange(ParseMap(context, line));
                    break;
                case "append":
                    if (ParseAppend(context, line) is { } append)
                    {
                        appends.Add(append);
                    }

                    break;
                case "children":
                    if (ParseChildren(context, line) is { } child)
                    {
                        children.Add(child);
                    }

                    break;
                default:
                    context.Error($"Unexpected '{line.Content}' in capture body", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name.Groups[1].Value, source, key, map, appends, children, header.Location);
    }

    static CaptureSourceSyntax ParseSource(ParserContext context, SourceLine line)
    {
        var kind = line.Content["source".Length..].Trim();
        var settings = new List<CaptureSourceSettingSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var settingName = LineText.FirstWord(child.Content);
            settings.Add(new(settingName, child.Content[settingName.Length..].Trim(), child.Location));
        }

        return new(kind, settings, line.Location);
    }

    static List<CaptureMapSyntax> ParseMap(ParserContext context, SourceLine line)
    {
        var entries = new List<CaptureMapSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MapEntryRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error($"Invalid map entry '{child.Content}' - expected '<property> = <source> [translate]'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            var translations = new List<CaptureTranslationSyntax>();
            if (match.Groups[3].Success)
            {
                while (context.TryPeekChild(child.Indent, out var translation))
                {
                    context.Reader.TakeSignificant();
                    var translationMatch = TranslationRegex().Match(translation.Content);
                    if (!translationMatch.Success)
                    {
                        context.Error($"Invalid translation '{translation.Content}' - expected '\"<source>\" => <target>'", translation.Location);
                        continue;
                    }

                    translations.Add(new(translationMatch.Groups[1].Value, translationMatch.Groups[2].Value, translation.Location));
                }
            }

            entries.Add(new(match.Groups[1].Value, match.Groups[2].Value, translations, child.Location));
        }

        return entries;
    }

    static CaptureAppendSyntax? ParseAppend(ParserContext context, SourceLine line)
    {
        var match = AppendRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid append declaration '{line.Content}' - expected 'append <EventType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        CaptureWhenSyntax? when = null;
        var mappings = new List<PropertyMappingSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "when")
            {
                when = ParseWhen(child);
                ParseMappings(context, child, mappings);
            }
            else if (!TryParseMapping(context, child, mappings))
            {
                context.Error($"Unexpected '{child.Content}' in append body", child.Location);
            }
        }

        return new(match.Groups[1].Value, when, mappings, line.Location);
    }

    static CaptureWhenSyntax ParseWhen(SourceLine line)
    {
        var trigger = line.Content["when".Length..].Trim();
        return trigger switch
        {
            "added" => new(CaptureWhenKind.Added, null, line.Location),
            "removed" => new(CaptureWhenKind.Removed, null, line.Location),
            "changed" => new(CaptureWhenKind.Changed, null, line.Location),
            _ => new(CaptureWhenKind.PropertyChanged, trigger, line.Location)
        };
    }

    static void ParseMappings(ParserContext context, SourceLine parent, List<PropertyMappingSyntax> mappings)
    {
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!TryParseMapping(context, child, mappings))
            {
                context.Error($"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
            }
        }
    }

    static bool TryParseMapping(ParserContext context, SourceLine line, List<PropertyMappingSyntax> mappings)
    {
        var match = MappingRegex().Match(line.Content);
        if (!match.Success)
        {
            return false;
        }

        mappings.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, line.Location), line.Location));
        return true;
    }

    static CaptureChildrenSyntax? ParseChildren(ParserContext context, SourceLine line)
    {
        var match = ChildrenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid children declaration '{line.Content}' - expected 'children <collection> identified by <key>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var appends = new List<CaptureAppendSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "append" && ParseAppend(context, child) is { } append)
            {
                appends.Add(append);
            }
            else if (LineText.FirstWord(child.Content) != "append")
            {
                context.Error($"Unexpected '{child.Content}' in children block - expected 'append <EventType>'", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(match.Groups[1].Value, match.Groups[2].Value, appends, line.Location);
    }

    [GeneratedRegex(@"^capture\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^([a-z_]\w*)\s*=\s*([\w.$]+)\s*(translate)?$", RegexOptions.None, 1000)]
    private static partial Regex MapEntryRegex();

    [GeneratedRegex("^\"([^\"]*)\"\\s*=>\\s*(\\w+)$", RegexOptions.None, 1000)]
    private static partial Regex TranslationRegex();

    [GeneratedRegex(@"^append\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex AppendRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();

    [GeneratedRegex(@"^children\s+([a-z_]\w*)\s+identified\s+by\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ChildrenRegex();
}
