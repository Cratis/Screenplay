// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>query</c> declarations with their parameters and authorization.
/// </summary>
internal static partial class QueryParser
{
    /// <summary>
    /// Parses a query from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>query</c> header.</param>
    /// <returns>The parsed <see cref="QuerySyntax"/>.</returns>
    public static QuerySyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error($"Invalid query declaration '{header.Content}' - expected 'query <Name> => <ReadModel>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), new(string.Empty, false, false, header.Location), null, [], null, header.Location);
        }

        var returnType = PropertyLineParser.ParseTypeRef(match.Groups[2].Value, header.Location);
        QueryParameterSyntax? by = null;
        var filters = new List<QueryParameterSyntax>();
        AuthorizeSyntax? authorize = null;
        var isObservable = false;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "by":
                    if (by is not null)
                    {
                        context.Error($"Query '{match.Groups[1].Value}' already declares a 'by' parameter - a query can have at most one", line.Location);
                        break;
                    }

                    by = ParseParameter(context, line, "by");
                    break;
                case "observable":
                    isObservable = ParseObservable(context, line, isObservable, match.Groups[1].Value);
                    break;
                case "filter":
                    if (ParseParameter(context, line, "filter") is { } filter)
                    {
                        filters.Add(filter);
                    }

                    break;
                case "authorize":
                    authorize = AuthorizeParser.Parse(context, line);
                    break;
                default:
                    context.Error($"Unexpected '{line.Content}' in query body", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, returnType, by, filters, authorize, header.Location, isObservable);
    }

    static bool ParseObservable(ParserContext context, SourceLine line, bool existing, string queryName)
    {
        if (line.Content != "observable")
        {
            context.Error($"Invalid observable declaration '{line.Content}' - expected 'observable'", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        if (existing)
        {
            context.Error($"Query '{queryName}' already declares 'observable' - declare it at most once", line.Location);
        }

        return true;
    }

    static QueryParameterSyntax? ParseParameter(ParserContext context, SourceLine line, string keyword)
    {
        var match = ParameterRegex().Match(line.Content[keyword.Length..].Trim());
        if (!match.Success)
        {
            context.Error($"Invalid '{keyword}' parameter '{line.Content}' - expected '{keyword} <name> <Type>'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.Location), line.Location);
    }

    [GeneratedRegex(@"^query\s+([A-Za-z_]\w*)\s*=>\s*([\w.]+(?:\[\])?\??)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^([a-z_]\w*)\s+([\w.]+(?:\[\])?\??)$", RegexOptions.None, 1000)]
    private static partial Regex ParameterRegex();
}
