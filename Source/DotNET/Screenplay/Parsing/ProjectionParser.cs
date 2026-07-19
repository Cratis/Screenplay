// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the projection sub-language - the body of a <c>projection</c> declaration.
/// </summary>
internal static partial class ProjectionParser
{
    /// <summary>
    /// Parses the projections of a standalone projection document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <returns>The parsed <see cref="ProjectionSyntax">projections</see>.</returns>
    public static IReadOnlyList<ProjectionSyntax> ParseDocument(ParserContext context)
    {
        var projections = new List<ProjectionSyntax>();
        while (context.Reader.PeekSignificant() is { } line)
        {
            if (line.Content.StartsWith("projection", StringComparison.Ordinal))
            {
                context.Reader.TakeSignificant();
                projections.Add(Parse(context, line));
            }
            else
            {
                context.Error($"Expected 'projection', got '{FirstWord(line.Content)}'", line.Location);
                context.Reader.TakeSignificant();
                context.SkipBlock(line.Indent);
            }
        }

        if (projections.Count == 0 && context.Diagnostics.Count == 0)
        {
            context.Error("Document must contain at least one projection", SourceLocation.Start);
        }

        return projections;
    }

    /// <summary>
    /// Parses a projection from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>projection</c> header.</param>
    /// <returns>The parsed <see cref="ProjectionSyntax"/>.</returns>
    public static ProjectionSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error($"Invalid projection declaration '{header.Content}' - expected 'projection <Name> [=> <ReadModel>]'", header.Location);
            context.SkipBlock(header.Indent);
            return new(FirstWord(header.Content), null, null, AutoMapMode.Inherit, null, [], header.Location);
        }

        var name = match.Groups[1].Value;
        var readModel = match.Groups[2].Success ? match.Groups[2].Value : null;
        string? sequence = null;
        var autoMap = AutoMapMode.Inherit;
        KeySyntax? key = null;
        var blocks = new List<ProjectionBlockSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (FirstWord(line.Content))
            {
                case "sequence":
                    sequence = line.Content["sequence".Length..].Trim();
                    break;
                case "automap":
                    autoMap = AutoMapMode.Enabled;
                    break;
                case "no" when line.Content == "no automap":
                    autoMap = AutoMapMode.Disabled;
                    break;
                case "key":
                    if (key is not null)
                    {
                        context.Error("Duplicate key directive - a projection can only declare one key", line.Location);
                    }

                    key = ParseKey(context, line);
                    break;
                default:
                    if (ParseBlock(context, line, nestedScope: false) is { } block)
                    {
                        blocks.Add(block);
                    }

                    break;
            }
        }

        if (blocks.Count == 0)
        {
            context.Error($"Projection '{name}' must contain at least one directive", header.Location);
        }

        return new(name, readModel, sequence, autoMap, key, blocks, header.Location);
    }

    static ProjectionBlockSyntax? ParseBlock(ParserContext context, SourceLine line, bool nestedScope)
    {
        switch (FirstWord(line.Content))
        {
            case "from":
                return ParseFrom(context, line);
            case "every":
                return ParseEvery(context, line);
            case "all":
                return ParseAll(context, line);
            case "join":
                return ParseJoin(context, line);
            case "children":
                return ParseChildren(context, line);
            case "nested":
                return ParseNested(context, line);
            case "remove":
                return ParseRemove(context, line);
            case "clear":
                if (!nestedScope)
                {
                    context.Error("'clear with' is only valid inside a nested block", line.Location);
                }

                return ParseClearWith(context, line);
            default:
                context.Error($"Unexpected '{FirstWord(line.Content)}' in projection body", line.Location);
                context.SkipBlock(line.Indent);
                return null;
        }
    }

    static FromSyntax ParseFrom(ParserContext context, SourceLine line)
    {
        var events = new List<EventSpecSyntax>();
        foreach (var spec in SplitTopLevel(line.Content["from".Length..], ','))
        {
            var text = spec.Trim();
            if (text.Length == 0)
            {
                context.Error("Expected an event type after 'from'", line.Location);
                continue;
            }

            var specMatch = EventSpecRegex().Match(text);
            if (!specMatch.Success)
            {
                context.Error($"Invalid event reference '{text}'", line.Location);
                continue;
            }

            var keyExpression = specMatch.Groups[2].Success
                ? ExpressionParser.ParseProjectionExpression(context, specMatch.Groups[2].Value, line.Location)
                : null;
            events.Add(new(Unescape(specMatch.Groups[1].Value), keyExpression, line.Location));
        }

        KeySyntax? key = null;
        ExpressionSyntax? parentKey = null;
        var mappings = new List<MappingSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (FirstWord(child.Content) == "key")
            {
                if (key is not null)
                {
                    context.Error("Duplicate key directive - a from block can only declare one key", child.Location);
                }

                key = ParseKey(context, child);
            }
            else if (FirstWord(child.Content) == "parent")
            {
                parentKey = ExpressionParser.ParseProjectionExpression(context, child.Content["parent".Length..], child.Location);
            }
            else if (ParseMapping(context, child) is { } mapping)
            {
                mappings.Add(mapping);
            }
        }

        return new(events, key, parentKey, mappings, line.Location);
    }

    static EverySyntax ParseEvery(ParserContext context, SourceLine line)
    {
        var includeChildren = true;
        var autoMap = AutoMapMode.Inherit;
        var mappings = ParseMappingBlock(context, line, ref autoMap, child =>
        {
            if (child.Content == "exclude children")
            {
                includeChildren = false;
                return true;
            }

            return false;
        });

        return new(mappings, includeChildren, autoMap, line.Location);
    }

    static AllSyntax ParseAll(ParserContext context, SourceLine line)
    {
        var autoMap = AutoMapMode.Inherit;
        var mappings = ParseMappingBlock(context, line, ref autoMap, _ => false);
        return new(mappings, autoMap, line.Location);
    }

    static JoinSyntax ParseJoin(ParserContext context, SourceLine line)
    {
        var match = JoinRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid join declaration '{line.Content}' - expected 'join <property> on <key>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, string.Empty, [], line.Location);
        }

        var events = new List<JoinEventSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var withMatch = WithRegex().Match(child.Content);
            if (!withMatch.Success)
            {
                context.Error($"Expected 'with <EventType>' in join block, got '{child.Content}'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            var autoMap = AutoMapMode.Inherit;
            var mappings = ParseMappingBlock(context, child, ref autoMap, _ => false);
            events.Add(new(Unescape(withMatch.Groups[1].Value), autoMap, mappings, child.Location));
        }

        return new(Unescape(match.Groups[1].Value), Unescape(match.Groups[2].Value), events, line.Location);
    }

    static ChildrenSyntax ParseChildren(ParserContext context, SourceLine line)
    {
        var match = ChildrenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid children declaration '{line.Content}' - expected 'children <collection> identified by <key>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, new RawExpressionSyntax(string.Empty, line.Location), AutoMapMode.Inherit, [], line.Location);
        }

        var identifiedBy = ExpressionParser.ParseProjectionExpression(context, match.Groups[2].Value, line.Location);
        var autoMap = AutoMapMode.Inherit;
        var blocks = ParseChildBlocks(context, line, ref autoMap, nestedScope: true);
        return new(Unescape(match.Groups[1].Value), identifiedBy, autoMap, blocks, line.Location);
    }

    static NestedSyntax ParseNested(ParserContext context, SourceLine line)
    {
        var match = NestedRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid nested declaration '{line.Content}' - expected 'nested <property>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, AutoMapMode.Inherit, [], line.Location);
        }

        var name = Unescape(match.Groups[1].Value);
        var autoMap = AutoMapMode.Inherit;
        var blocks = ParseChildBlocks(context, line, ref autoMap, nestedScope: true);
        if (!blocks.OfType<FromSyntax>().Any())
        {
            context.Error($"Nested block '{name}' must contain at least one 'from' directive", line.Location);
        }

        return new(name, autoMap, blocks, line.Location);
    }

    static List<ProjectionBlockSyntax> ParseChildBlocks(ParserContext context, SourceLine line, ref AutoMapMode autoMap, bool nestedScope)
    {
        var blocks = new List<ProjectionBlockSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content == "automap")
            {
                autoMap = AutoMapMode.Enabled;
            }
            else if (child.Content == "no automap")
            {
                autoMap = AutoMapMode.Disabled;
            }
            else if (ParseBlock(context, child, nestedScope) is { } block)
            {
                blocks.Add(block);
            }
        }

        return blocks;
    }

    static ProjectionBlockSyntax? ParseRemove(ParserContext context, SourceLine line)
    {
        var viaJoin = RemoveViaJoinRegex().Match(line.Content);
        if (viaJoin.Success)
        {
            var joinKey = viaJoin.Groups[2].Success
                ? ExpressionParser.ParseProjectionExpression(context, viaJoin.Groups[2].Value, line.Location)
                : null;
            return new RemoveViaJoinSyntax(Unescape(viaJoin.Groups[1].Value), joinKey, line.Location);
        }

        var match = RemoveWithRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid remove declaration '{line.Content}' - expected 'remove with <EventType>' or 'remove via join on <EventType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var key = match.Groups[2].Success
            ? ExpressionParser.ParseProjectionExpression(context, match.Groups[2].Value, line.Location)
            : null;

        ExpressionSyntax? parentKey = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (FirstWord(child.Content) == "parent")
            {
                parentKey = ExpressionParser.ParseProjectionExpression(context, child.Content["parent".Length..], child.Location);
            }
            else
            {
                context.Error($"Unexpected '{child.Content}' in remove block - only 'parent' is allowed", child.Location);
            }
        }

        return new RemoveWithSyntax(Unescape(match.Groups[1].Value), key, parentKey, line.Location);
    }

    static ClearWithSyntax ParseClearWith(ParserContext context, SourceLine line)
    {
        var match = ClearWithRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid clear declaration '{line.Content}' - expected 'clear with <EventType>'", line.Location);
            return new(string.Empty, line.Location);
        }

        return new(Unescape(match.Groups[1].Value), line.Location);
    }

    static KeySyntax ParseKey(ParserContext context, SourceLine line)
    {
        var text = line.Content["key".Length..].Trim();
        if (context.TryPeekChild(line.Indent, out _))
        {
            var type = text.TrimEnd('{').Trim();
            var parts = new List<KeyPartSyntax>();
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                if (child.Content == "}")
                {
                    continue;
                }

                var partMatch = AssignmentRegex().Match(child.Content);
                if (!partMatch.Success)
                {
                    context.Error($"Invalid composite key part '{child.Content}' - expected '<property> = <expression>'", child.Location);
                    continue;
                }

                var expression = ExpressionParser.ParseProjectionExpression(context, partMatch.Groups[2].Value, child.Location);
                if (expression is TemplateExpressionSyntax)
                {
                    context.Error("Template expressions are not allowed in composite keys", child.Location);
                }

                parts.Add(new(Unescape(partMatch.Groups[1].Value), expression, child.Location));
            }

            if (context.Reader.PeekSignificant() is { } closing && closing.Indent == line.Indent && closing.Content == "}")
            {
                context.Reader.TakeSignificant();
            }

            if (parts.Count == 0)
            {
                context.Error("Composite keys must contain at least one part", line.Location);
            }

            return new CompositeKeySyntax(type, parts, line.Location);
        }

        return new ExpressionKeySyntax(ExpressionParser.ParseProjectionExpression(context, text, line.Location), line.Location);
    }

    static List<MappingSyntax> ParseMappingBlock(ParserContext context, SourceLine line, ref AutoMapMode autoMap, Func<SourceLine, bool> extras)
    {
        var mappings = new List<MappingSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content == "automap")
            {
                autoMap = AutoMapMode.Enabled;
            }
            else if (child.Content == "no automap")
            {
                autoMap = AutoMapMode.Disabled;
            }
            else if (!extras(child) && ParseMapping(context, child) is { } mapping)
            {
                mappings.Add(mapping);
            }
        }

        return mappings;
    }

    static MappingSyntax? ParseMapping(ParserContext context, SourceLine line)
    {
        var counter = CounterRegex().Match(line.Content);
        if (counter.Success)
        {
            var property = Unescape(counter.Groups[2].Value);
            return counter.Groups[1].Value switch
            {
                "increment" => new IncrementMappingSyntax(property, line.Location),
                "decrement" => new DecrementMappingSyntax(property, line.Location),
                _ => new CountMappingSyntax(property, line.Location)
            };
        }

        var arithmetic = ArithmeticRegex().Match(line.Content);
        if (arithmetic.Success)
        {
            var property = Unescape(arithmetic.Groups[2].Value);
            var value = ExpressionParser.ParseProjectionExpression(context, arithmetic.Groups[3].Value, line.Location);
            return arithmetic.Groups[1].Value == "add"
                ? new AddMappingSyntax(property, value, line.Location)
                : new SubtractMappingSyntax(property, value, line.Location);
        }

        var set = SetRegex().Match(line.Content);
        if (set.Success)
        {
            var source = ExpressionParser.ParseProjectionExpression(context, set.Groups[2].Value, line.Location);
            return new SetMappingSyntax(Unescape(set.Groups[1].Value), source, line.Location);
        }

        var assignment = AssignmentRegex().Match(line.Content);
        if (assignment.Success)
        {
            var source = ExpressionParser.ParseProjectionExpression(context, assignment.Groups[2].Value, line.Location);
            return new SetMappingSyntax(Unescape(assignment.Groups[1].Value), source, line.Location);
        }

        context.Error($"Invalid mapping '{line.Content}'", line.Location);
        return null;
    }

    static string FirstWord(string content) => LineText.FirstWord(content);

    static string Unescape(string identifier) => LineText.Unescape(identifier);

    static IEnumerable<string> SplitTopLevel(string text, char separator) => LineText.SplitTopLevel(text, separator);

    [GeneratedRegex(@"^projection\s+(@?[\w.]+)\s*(?:=>\s*([\w.]+))?$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(@?[\w.]+)(?:\s+key\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex EventSpecRegex();

    [GeneratedRegex(@"^join\s+(@?[\w.]+)\s+on\s+(@?[\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex JoinRegex();

    [GeneratedRegex(@"^with\s+(@?[\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex WithRegex();

    [GeneratedRegex(@"^children\s+(@?[\w.]+)\s+identified\s+by\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ChildrenRegex();

    [GeneratedRegex(@"^nested\s+(@?[\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex NestedRegex();

    [GeneratedRegex(@"^remove\s+with\s+(@?[\w.]+)(?:\s+key\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex RemoveWithRegex();

    [GeneratedRegex(@"^remove\s+via\s+join\s+on\s+(@?[\w.]+)(?:\s+key\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex RemoveViaJoinRegex();

    [GeneratedRegex(@"^clear\s+with\s+(@?[\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex ClearWithRegex();

    [GeneratedRegex(@"^(increment|decrement|count)\s+(@?[$\w.]+)$", RegexOptions.None, 1000)]
    private static partial Regex CounterRegex();

    [GeneratedRegex(@"^(add|subtract)\s+(@?[$\w.]+)\s+by\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ArithmeticRegex();

    [GeneratedRegex(@"^set\s+(@?[$\w.]+)\s+(?:=|to)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex SetRegex();

    [GeneratedRegex(@"^(@?[$\w.@]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex AssignmentRegex();
}
