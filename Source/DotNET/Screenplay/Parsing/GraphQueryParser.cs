// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a query that reads a graph: <c language="screenplay">query &lt;Name&gt; =&gt; [observable] &lt;ReadModel&gt;[] graph &lt;Graph&gt;</c>.
/// </summary>
internal static partial class GraphQueryParser
{
    /// <summary>
    /// Gets whether a <c>query</c> header declares a graph query.
    /// </summary>
    /// <param name="content">The header line.</param>
    /// <returns><c>true</c> when it ends in <c>graph &lt;Graph&gt;</c>.</returns>
    public static bool IsGraphHeader(string content) => GraphSuffixRegex().IsMatch(content);

    /// <summary>
    /// Parses a graph query and its body.
    /// </summary>
    /// <param name="context">The parser context.</param>
    /// <param name="header">The header line.</param>
    /// <returns>The parsed <see cref="GraphQuerySyntax"/>.</returns>
    public static GraphQuerySyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid graph query declaration '{header.Content}' - expected 'query <Name> => [observable] <ReadModel>[] graph <Graph>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), new(string.Empty, false, false, header.Location), string.Empty, null, [], null, [], [], [], header.Location);
        }

        var name = match.Groups[1].Value;
        var returnType = PropertyLineParser.ParseTypeRef(match.Groups[3].Value, header.LocationAt(match.Groups[3].Index));
        var graph = match.Groups[4].Value;
        QueryParameterSyntax? by = null;
        var filters = new List<QueryParameterSyntax>();
        AuthorizeSyntax? authorize = null;
        string? description = null;
        var matches = new List<GraphMatchSyntax>();
        var conditions = new List<GraphConditionSyntax>();
        var returns = new List<GraphReturnSyntax>();
        var collects = new List<GraphCollectSyntax>();
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"Query '{name}'");
                    break;
                case "by" when line.Content != "by":
                    if (QueryParser.ParseParameter(context, line, "by") is { } parameter) by = parameter;
                    break;
                case "filter":
                    if (QueryParser.ParseParameter(context, line, "filter") is { } filter) filters.Add(filter);
                    break;
                case "authorize":
                    authorize = AuthorizeParser.Combine(authorize, AuthorizeParser.Parse(context, line), context);
                    break;
                case "match":
                case "optional" when line.Content.StartsWith("optional match ", StringComparison.Ordinal):
                    if (ParseMatch(context, line) is { } parsed) matches.Add(parsed);
                    break;
                case "where":
                    ParseWhere(context, line, conditions);
                    break;
                case "return":
                    ParseReturn(context, line, returns, collects);
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidGraphQuery, $"Unexpected '{line.Content}' in graph query body - expected description, by, filter, authorize, match, optional match, where or return", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        if (matches.Count == 0 || matches.TrueForAll(candidate => candidate.IsOptional))
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"Graph query '{name}' needs at least one 'match' (not 'optional match')", header.Location);
        }

        if (returns.Count == 0 && collects.Count == 0)
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"Graph query '{name}' needs a 'return' block that assigns the read model's properties", header.Location);
        }

        return new(name, returnType, graph, by, filters, authorize, matches, conditions, returns, header.Location)
        {
            IsObservable = match.Groups[2].Success,
            Description = description,
            Collects = collects,
            DirectiveLocations = directiveLocations
        };
    }

    static GraphMatchSyntax? ParseMatch(ParserContext context, SourceLine line)
    {
        var optional = line.Content.StartsWith("optional", StringComparison.Ordinal);
        var prefixLength = optional ? "optional match ".Length : "match ".Length;
        var text = line.Content[prefixLength..].TrimEnd();
        var offset = prefixLength;
        var start = StartRegex().Match(text);
        if (!start.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid pattern '{text}' - expected '[alias:]Label (-[alias:]TYPE-> | <-[alias:]TYPE-) [alias:]Label ...'", line.Location);
            return null;
        }

        var startNode = new GraphPatternNodeSyntax(Alias(start.Groups[1]), start.Groups[2].Value, line.LocationAt(offset + start.Groups[2].Index));
        var hops = new List<GraphPatternHopSyntax>();
        var index = start.Length;
        while (index < text.Length)
        {
            var hop = HopRegex().Match(text[index..]);
            if (!hop.Success)
            {
                context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid pattern '{text}' - at '{text[index..].Trim()}' expected '-[alias:]TYPE-> [alias:]Label' or '<-[alias:]TYPE- [alias:]Label'", line.Location);
                return null;
            }

            var outgoing = hop.Groups["out"].Success;
            var edgeAlias = outgoing ? hop.Groups["oa"] : hop.Groups["ia"];
            var edgeType = outgoing ? hop.Groups["ot"] : hop.Groups["it"];
            var node = new GraphPatternNodeSyntax(Alias(hop.Groups["na"]), hop.Groups["nl"].Value, line.LocationAt(offset + index + hop.Groups["nl"].Index));
            hops.Add(new(Alias(edgeAlias), edgeType.Value, outgoing, node, line.LocationAt(offset + index)));
            index += hop.Length;
        }

        if (hops.Count == 0 && optional)
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"An 'optional match' needs at least one edge: '{text}'", line.Location);
            return null;
        }

        return new(optional, startNode, hops, line.Location);
    }

    static string? Alias(Group group) => group.Success ? group.Value : null;

    static void ParseWhere(ParserContext context, SourceLine line, List<GraphConditionSyntax> conditions)
    {
        var text = line.Content["where".Length..].Trim();
        if (text.Length == 0)
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, "'where' needs a condition: 'where <alias>.<property> = <$argument | literal>'", line.Location);
            return;
        }

        foreach (var part in SplitOnAnd(text))
        {
            var condition = ConditionRegex().Match(part.Trim());
            if (!condition.Success)
            {
                context.Error(DiagnosticCodes.UnsupportedGraphQueryConstruct, $"Unsupported condition '{part.Trim()}' - a graph query condition is '<alias>.<property> = <$argument | literal>', joined with 'and'", line.Location);
                continue;
            }

            var raw = condition.Groups[3].Value.Trim();
            var value = raw.StartsWith('$') && ArgumentRegex().IsMatch(raw)
                ? new PathExpressionSyntax(raw[1..], line.Location)
                : ExpressionParser.ParseLiteral(context, raw, line.Location);
            if (value is null && raw.Contains(" or ", StringComparison.Ordinal))
            {
                context.Error(DiagnosticCodes.UnsupportedGraphQueryConstruct, $"Unsupported condition '{part.Trim()}' - this version joins conditions with 'and' only", line.Location);
                continue;
            }

            if (value is null)
            {
                context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid condition value '{raw}' - expected '$argument', a string, a number, 'true' or 'false'", line.Location);
                continue;
            }

            conditions.Add(new(condition.Groups[1].Value, condition.Groups[2].Value, value, line.Location));
        }
    }

    static IEnumerable<string> SplitOnAnd(string text)
    {
        var start = 0;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quote != '\0')
            {
                if (character == '\\') index++;
                else if (character == quote) quote = '\0';
            }
            else if (character is '"' or '\'')
            {
                quote = character;
            }
            else if (character == ' ' && string.CompareOrdinal(text, index, " and ", 0, 5) == 0)
            {
                yield return text[start..index];
                start = index + 5;
                index += 4;
            }
        }

        yield return text[start..];
    }

    static void ParseReturn(ParserContext context, SourceLine line, List<GraphReturnSyntax> returns, List<GraphCollectSyntax> collects)
    {
        if (line.Content != "return")
        {
            context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid '{line.Content}' - expected 'return' followed by one '<property> = <alias>.<property>' line per read-model property", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var collect = CollectRegex().Match(child.Content);
            if (collect.Success)
            {
                if (ParseCollect(context, child, collect) is { } parsed) collects.Add(parsed);
                continue;
            }

            var assignment = ReturnRegex().Match(child.Content);
            if (!assignment.Success)
            {
                context.Error(DiagnosticCodes.InvalidGraphQuery, $"Invalid return '{child.Content}' - expected '<property> = <alias>.<property>'", child.Location);
                continue;
            }

            returns.Add(new(assignment.Groups[1].Value, assignment.Groups[2].Value, assignment.Groups[3].Value, child.Location));
        }
    }

    // '<property> = collect <root> -[alias:]TYPE-> [alias:]Label' and one '<property> = <alias>.<property>' line per row property beneath it.
    static GraphCollectSyntax? ParseCollect(ParserContext context, SourceLine line, Match collect)
    {
        var property = collect.Groups[1].Value;
        var text = collect.Groups[2].Value.TrimEnd();
        var offset = collect.Groups[2].Index;
        var start = CollectStartRegex().Match(text);
        var hop = start.Success ? HopRegex().Match(text[start.Length..]) : Match.Empty;
        var rows = new List<GraphReturnSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (CollectRegex().IsMatch(child.Content))
            {
                context.Error(DiagnosticCodes.NestedGraphCollect, $"'{child.Content}' collects inside a collect - this version collects one level of rows; follow the edge from the root in a collect of its own", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            var assignment = ReturnRegex().Match(child.Content);
            if (!assignment.Success)
            {
                context.Error(DiagnosticCodes.InvalidGraphCollect, $"Invalid collected row '{child.Content}' - expected '<property> = <alias>.<property>' naming the collected node or edge", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            rows.Add(new(assignment.Groups[1].Value, assignment.Groups[2].Value, assignment.Groups[3].Value, child.Location));
        }

        if (!start.Success || !hop.Success || start.Length + hop.Length != text.Length)
        {
            context.Error(DiagnosticCodes.InvalidGraphCollect, $"Invalid collect '{text}' - expected '<root> -[alias:]TYPE-> [alias:]Label' or '<root> <-[alias:]TYPE- [alias:]Label': one edge from the query's root", line.Location);
            return null;
        }

        if (rows.Count == 0)
        {
            context.Error(DiagnosticCodes.InvalidGraphCollect, $"Collect '{property}' needs one '<property> = <alias>.<property>' line per property of its row", line.Location);
        }

        var outgoing = hop.Groups["out"].Success;
        var edgeAlias = outgoing ? hop.Groups["oa"] : hop.Groups["ia"];
        var edgeType = outgoing ? hop.Groups["ot"] : hop.Groups["it"];
        var hopOffset = offset + start.Length;
        var node = new GraphPatternNodeSyntax(Alias(hop.Groups["na"]), hop.Groups["nl"].Value, line.LocationAt(hopOffset + hop.Groups["nl"].Index));
        var followed = new GraphPatternHopSyntax(Alias(edgeAlias), edgeType.Value, outgoing, node, line.LocationAt(hopOffset));
        return new(property, start.Groups[1].Value, followed, rows, line.Location);
    }

    [GeneratedRegex(@"^query\s+[A-Za-z_]\w*\s*=>\s*(?:observable\s+)?[\w.]+(?:\[\])?(?:\?|\s+optional)?\s+graph\s+@?[\w.]+$", RegexOptions.NonBacktracking)]
    private static partial Regex GraphSuffixRegex();

    [GeneratedRegex(@"^query\s+([A-Za-z_]\w*)\s*=>\s*(observable\s+)?([\w.]+(?:\[\])?(?:\?|\s+optional)?)\s+graph\s+(@?[\w.]+)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(?:([A-Za-z_]\w*):)?([A-Za-z_]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex StartRegex();

    [GeneratedRegex(@"^\s*(?:-(?:(?<oa>[A-Za-z_]\w*):)?(?<ot>[A-Za-z_]\w*)->(?<out>)|<-(?:(?<ia>[A-Za-z_]\w*):)?(?<it>[A-Za-z_]\w*)-)\s*(?:(?<na>[A-Za-z_]\w*):)?(?<nl>[A-Za-z_]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex HopRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\.([A-Za-z_]\w*)\s*=\s*(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex ConditionRegex();

    [GeneratedRegex(@"^\$[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex ArgumentRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s*=\s*([A-Za-z_]\w*)\.([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ReturnRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s*=\s*collect(?:\s+(.*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex CollectRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)", RegexOptions.NonBacktracking)]
    private static partial Regex CollectStartRegex();
}
