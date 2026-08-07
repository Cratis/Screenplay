// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>query</c> declarations with their description, parameters, authorization and performer.
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
            context.Error(DiagnosticCodes.InvalidQueryDeclaration, $"Invalid query declaration '{header.Content}' - expected 'query <Name> => [observable] <ReadModel>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), new(string.Empty, false, false, header.Location), null, [], null, header.Location);
        }

        var name = match.Groups[1].Value;
        var isObservable = match.Groups[2].Success;
        var returnType = PropertyLineParser.ParseTypeRef(match.Groups[3].Value, header.Location);
        QueryParameterSyntax? by = null;
        var filters = new List<QueryParameterSyntax>();
        AuthorizeSyntax? authorize = null;
        PerformerSyntax? performer = null;
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"Query '{name}'");
                    break;
                case "by":
                    by = ParseParameter(context, line, "by") ?? by;
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
                case "performer":
                    performer = ParsePerformer(context, line, performer, name);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownQueryDirective, $"Unexpected '{line.Content}' in query body - expected description, by, filter, authorize or performer", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, returnType, by, filters, authorize, header.Location, description, performer, isObservable);
    }

    static QueryParameterSyntax? ParseParameter(ParserContext context, SourceLine line, string keyword)
    {
        var match = ParameterRegex().Match(line.Content[keyword.Length..].Trim());
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidQueryParameter, $"Invalid '{keyword}' parameter '{line.Content}' - expected '{keyword} <name> <Type> [from <source>]'", line.Location);
            return null;
        }

        var source = match.Groups[3].Success
            ? ExpressionParser.ParseMappingSource(context, match.Groups[3].Value, line.Location)
            : null;

        return new(match.Groups[1].Value, PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.Location), line.Location, source);
    }

    static PerformerSyntax? ParsePerformer(ParserContext context, SourceLine line, PerformerSyntax? existing, string queryName)
    {
        if (line.Content != "performer")
        {
            context.Error(DiagnosticCodes.InvalidPerformerDeclaration, $"Invalid performer declaration '{line.Content}' - expected 'performer'", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicatePerformer, $"Query '{queryName}' already declares a performer - a query can have at most one", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        if (!context.TryPeekChild(line.Indent, out var body))
        {
            context.Error(DiagnosticCodes.PerformerWithoutImplementation, "Expected a 'file' directive or an inline code block in the performer", line.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (LineText.FirstWord(body.Content) == "file")
        {
            return new(new(body.Content["file".Length..].Trim(), body.Location), null, line.Location);
        }

        if (context.Languages.InlineLanguages.Contains(body.Content))
        {
            var code = CodeBlockParser.Parse(context, body.Content, body);
            return code is null ? null : new PerformerSyntax(null, code, line.Location);
        }

        context.Error(DiagnosticCodes.UnknownPerformerDirective, $"Unexpected '{body.Content}' in performer - expected 'file <path>' or an inline code block", body.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    [GeneratedRegex(@"^query\s+([A-Za-z_]\w*)\s*=>\s*(observable\s+)?([\w.]+(?:\[\])?\??)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^([a-z_]\w*)\s+([\w.]+(?:\[\])?\??)(?:\s+from\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex ParameterRegex();
}
