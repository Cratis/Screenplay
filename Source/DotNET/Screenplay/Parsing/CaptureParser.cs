// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>capture</c> declarations - the change data capture sub-language.
/// </summary>
internal static partial class CaptureParser
{
    /// <summary>
    /// Parses the captures of a standalone capture document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <returns>The parsed <see cref="CaptureSyntax">captures</see>.</returns>
    public static IReadOnlyList<CaptureSyntax> ParseDocument(ParserContext context)
    {
        var captures = new List<CaptureSyntax>();
        while (context.Reader.PeekSignificant() is { } line)
        {
            if (line.Content.StartsWith("capture", StringComparison.Ordinal))
            {
                context.Reader.TakeSignificant();
                captures.Add(Parse(context, line));
            }
            else
            {
                context.Error($"Expected 'capture', got '{LineText.FirstWord(line.Content)}'", line.Location);
                context.Reader.TakeSignificant();
                context.SkipBlock(line.Indent);
            }
        }

        if (captures.Count == 0 && context.Diagnostics.Count == 0)
        {
            context.Error("Document must contain at least one capture", SourceLocation.Start);
        }

        return captures;
    }

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
        var map = new List<CaptureMapOperationSyntax>();
        var appends = new List<CaptureAppendSyntax>();
        var children = new List<CaptureChildrenSyntax>();
        var nested = new List<CaptureNestedSyntax>();

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
                case "nested":
                    if (ParseNested(context, line) is { } nestedBlock)
                    {
                        nested.Add(nestedBlock);
                    }

                    break;
                default:
                    context.Error($"Unexpected '{line.Content}' in capture body", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name.Groups[1].Value, source, key, map, appends, children, nested, header.Location);
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

    static List<CaptureMapOperationSyntax> ParseMap(ParserContext context, SourceLine line)
    {
        var operations = new List<CaptureMapOperationSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "split")
            {
                if (ParseSplit(context, child) is { } split)
                {
                    operations.Add(split);
                }

                continue;
            }

            if (ParseMapEntry(context, child) is { } entry)
            {
                operations.Add(entry);
            }
        }

        return operations;
    }

    static CaptureMapEntrySyntax? ParseMapEntry(ParserContext context, SourceLine line)
    {
        var match = MapEntryRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid map entry '{line.Content}' - expected '<property> = <source> [translate]'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var (sourceText, hasTranslate) = SplitTranslateSuffix(match.Groups[2].Value.Trim());
        var source = ExpressionParser.ParseProjectionExpression(context, sourceText, line.Location);

        var translations = new List<CaptureTranslationSyntax>();
        if (hasTranslate)
        {
            while (context.TryPeekChild(line.Indent, out var translation))
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

        return new(match.Groups[1].Value, source, translations, line.Location);
    }

    static (string Source, bool HasTranslate) SplitTranslateSuffix(string raw)
    {
        if (raw.StartsWith('`'))
        {
            var closing = raw.IndexOf('`', 1);
            if (closing == -1)
            {
                return (raw, false);
            }

            return (raw[..(closing + 1)], raw[(closing + 1)..].Trim() == "translate");
        }

        const string suffix = " translate";
        return raw.EndsWith(suffix, StringComparison.Ordinal)
            ? (raw[..^suffix.Length].TrimEnd(), true)
            : (raw, false);
    }

    static CaptureSplitSyntax? ParseSplit(ParserContext context, SourceLine line)
    {
        var match = SplitRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid split operation '{line.Content}' - expected 'split <property> by \"<separator>\"'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var source = ExpressionParser.ParseProjectionExpression(context, match.Groups[1].Value, line.Location);
        var targets = new List<string>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (!TargetRegex().IsMatch(child.Content))
            {
                context.Error($"Invalid split target '{child.Content}' - expected a property path", child.Location);
                continue;
            }

            targets.Add(child.Content);
        }

        return new(source, match.Groups[2].Value, targets, line.Location);
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
        var tags = new List<TagSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "when")
            {
                when = ParseWhen(context, child);
                ParseMappings(context, child, mappings, tags);
            }
            else if (LineText.FirstWord(child.Content) == "tag")
            {
                if (TagParser.Parse(context, child) is { } tag)
                {
                    tags.Add(tag);
                }
            }
            else if (!TryParseMapping(context, child, mappings))
            {
                context.Error($"Unexpected '{child.Content}' in append body", child.Location);
            }
        }

        return new(match.Groups[1].Value, when, mappings, line.Location, tags);
    }

    static CaptureWhenSyntax? ParseWhen(ParserContext context, SourceLine line)
    {
        var trigger = line.Content["when".Length..].Trim();
        if (trigger.Length == 0)
        {
            context.Error("Expected a trigger after 'when'", line.Location);
            return null;
        }

        switch (trigger)
        {
            case "added":
                return new(CaptureWhenKind.Added, [], null, null, null, line.Location);
            case "removed":
                return new(CaptureWhenKind.Removed, [], null, null, null, line.Location);
        }

        if (trigger.StartsWith('`'))
        {
            if (!trigger.EndsWith('`') || trigger.Length < 2)
            {
                context.Error("Unterminated template expression in 'when' - expected a closing backtick", line.Location);
            }

            return new(CaptureWhenKind.Expression, [], null, null, trigger, line.Location);
        }

        return ParseWhenPropertyClause(context, line, trigger);
    }

    static CaptureWhenSyntax? ParseWhenPropertyClause(ParserContext context, SourceLine line, string trigger)
    {
        var tokens = WhenTokenRegex().Matches(trigger).Select(_ => _.Value).ToList();
        if (tokens.Count == 0)
        {
            context.Error($"Invalid 'when' clause '{line.Content}'", line.Location);
            return null;
        }

        var property = Unquote(tokens[0]);
        if (tokens.Count == 1)
        {
            return new(CaptureWhenKind.PropertyChanged, [property], null, null, null, line.Location);
        }

        if (tokens[1] == "from")
        {
            if (tokens.Count == 5 && tokens[3] == "to")
            {
                return new(CaptureWhenKind.ValueTransition, [property], Unquote(tokens[2]), Unquote(tokens[4]), null, line.Location);
            }

            context.Error($"Invalid 'when ... from ... to ...' clause '{line.Content}' - expected 'when <Path> from <value> to <value>'", line.Location);
            return null;
        }

        if (tokens[1] == "or" || tokens[1] == "and")
        {
            return ParseWhenCombinator(context, line, tokens, property);
        }

        context.Error(
            $"Invalid 'when' clause '{line.Content}' - expected 'added', 'removed', a template, or '<Path> [from <value> to <value> | (or|and) <Path>...]'",
            line.Location);
        return null;
    }

    static CaptureWhenSyntax? ParseWhenCombinator(ParserContext context, SourceLine line, List<string> tokens, string property)
    {
        var combinator = tokens[1];
        var properties = new List<string> { property };
        for (var i = 1; i < tokens.Count - 1; i += 2)
        {
            if (tokens[i] != combinator)
            {
                context.Error($"Cannot mix 'and' and 'or' in a single 'when' clause '{line.Content}'", line.Location);
                return null;
            }

            properties.Add(Unquote(tokens[i + 1]));
        }

        if (tokens.Count % 2 == 0)
        {
            context.Error($"Invalid 'when' clause '{line.Content}' - expected a property after '{combinator}'", line.Location);
            return null;
        }

        var kind = combinator == "or" ? CaptureWhenKind.LogicalOr : CaptureWhenKind.LogicalAnd;
        return new(kind, properties, null, null, null, line.Location);
    }

    static string Unquote(string token) =>
        token.Length >= 2 && token.StartsWith('"') && token.EndsWith('"') ? token[1..^1] : token;

    static void ParseMappings(ParserContext context, SourceLine parent, List<PropertyMappingSyntax> mappings, List<TagSyntax> tags)
    {
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "tag")
            {
                if (TagParser.Parse(context, child) is { } tag)
                {
                    tags.Add(tag);
                }
            }
            else if (!TryParseMapping(context, child, mappings))
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

        var (map, appends) = ParseMapAndAppends(context, line, "children");
        return new(match.Groups[1].Value, match.Groups[2].Value, map, appends, line.Location);
    }

    static CaptureNestedSyntax? ParseNested(ParserContext context, SourceLine line)
    {
        var match = NestedRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid nested declaration '{line.Content}' - expected 'nested <Property>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var (map, appends) = ParseMapAndAppends(context, line, "nested");
        return new(match.Groups[1].Value, map, appends, line.Location);
    }

    static (IEnumerable<CaptureMapOperationSyntax> Map, List<CaptureAppendSyntax> Appends) ParseMapAndAppends(ParserContext context, SourceLine line, string blockName)
    {
        IEnumerable<CaptureMapOperationSyntax> map = [];
        var appends = new List<CaptureAppendSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "map":
                    map = ParseMap(context, child);
                    break;
                case "append":
                    if (ParseAppend(context, child) is { } append)
                    {
                        appends.Add(append);
                    }

                    break;
                default:
                    context.Error($"Unexpected '{child.Content}' in {blockName} block - expected 'map' or 'append <EventType>'", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return (map, appends);
    }

    [GeneratedRegex(@"^capture\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^([a-z_]\w*)\s*=\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MapEntryRegex();

    [GeneratedRegex("^\"([^\"]*)\"\\s*=>\\s*(\\w+)$", RegexOptions.None, 1000)]
    private static partial Regex TranslationRegex();

    [GeneratedRegex("^split\\s+(\\S+)\\s+by\\s+\"([^\"]*)\"$", RegexOptions.None, 1000)]
    private static partial Regex SplitRegex();

    [GeneratedRegex(@"^[\w.]+$", RegexOptions.None, 1000)]
    private static partial Regex TargetRegex();

    [GeneratedRegex(@"^append\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex AppendRegex();

    [GeneratedRegex("\"[^\"]*\"|[\\w.]+", RegexOptions.None, 1000)]
    private static partial Regex WhenTokenRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();

    [GeneratedRegex(@"^children\s+([a-z_]\w*)\s+identified\s+by\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ChildrenRegex();

    [GeneratedRegex(@"^nested\s+([\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex NestedRegex();
}
