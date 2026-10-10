// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses top-level identity authoring metadata.
/// </summary>
internal static partial class IdentityParser
{
    /// <summary>
    /// Parses a block, retaining the first valid declaration.
    /// </summary>
    /// <param name="context">The parser context.</param>
    /// <param name="header">The consumed header.</param>
    /// <param name="existing">The previously declared block.</param>
    /// <returns>The identity metadata.</returns>
    public static IdentitySyntax? Parse(ParserContext context, SourceLine header, IdentitySyntax? existing)
    {
        if (header.Content != "identity")
        {
            context.Error(DiagnosticCodes.InvalidIdentityDeclaration, $"Invalid identity declaration '{header.Content}' - expected 'identity'", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateIdentity, "The document already declares an identity block - a document can have at most one", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        var details = new List<IdentityDetailSyntax>();
        string? description = null;
        var directives = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (DescriptionParser.TryParse(context, line, ref description, "Identity", directives)) continue;
            var match = DetailRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidIdentityDetail, $"Invalid identity detail '{line.Content}' - expected '<name> <Type> [from claim \"<name>\" | from query <Query> by <expression>]'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var type = PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.LocationAt(match.Groups[2].Index));
            PropertyLineParser.ReportLegacyOptionalSuffix(context, type, line);
            var source = match.Groups[3].Success ? ParseSource(context, line, match.Groups[3].Value) : ParseEscape(context, line);
            if (source is not null) details.Add(new(LineText.Unescape(match.Groups[1].Value), type, source, line.Location));
            RejectBody(context, line, match.Groups[3].Success ? DiagnosticCodes.IdentitySourceWithBody : DiagnosticCodes.InvalidIdentityDetail);
        }

        return new(details, header.Location) { Description = description, DirectiveLocations = directives };
    }

    static IdentitySourceSyntax? ParseSource(ParserContext context, SourceLine line, string text)
    {
        if (ClaimRegex().Match(text) is { Success: true } claim)
        {
            return new ClaimIdentitySourceSyntax(StringLiteral.Unescape(claim.Groups[1].Value), line.Location);
        }

        if (QueryRegex().Match(text) is { Success: true } query)
        {
            if (!query.Groups[2].Success)
            {
                context.Error(DiagnosticCodes.IdentityQueryWithoutKey, "An identity query source requires 'by <expression>'", line.Location);
                return null;
            }

            return new QueryIdentitySourceSyntax(query.Groups[1].Value, ExpressionParser.ParseMappingSource(context, query.Groups[2].Value, line.Location), line.Location);
        }

        var kind = LineText.FirstWord(text);
        context.Error(
            kind == "claim" || kind == "query" ? DiagnosticCodes.InvalidIdentityDetail : DiagnosticCodes.UnknownIdentitySource,
            $"Invalid identity source '{text}' - expected 'claim \"<name>\"' or 'query <Query> by <expression>'",
            line.Location);
        return null;
    }

    static IdentitySourceSyntax? ParseEscape(ParserContext context, SourceLine detail)
    {
        if (!context.TryPeekChild(detail.Indent, out var body))
        {
            context.Error(DiagnosticCodes.IdentityDetailWithoutSource, "An identity detail requires a claim, query, inline code block or file source", detail.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (FileReferenceParser.IsDirective(body))
        {
            var file = FileReferenceParser.Parse(context, body);
            RejectBody(context, body, DiagnosticCodes.InvalidIdentityDetail);
            return file is null ? null : new FileIdentitySourceSyntax(file, body.Location);
        }

        if (CodeBlockParser.IsCodeLine(context, body))
        {
            var code = CodeBlockParser.Parse(context, body);
            return code is null ? null : new CodeIdentitySourceSyntax(code, body.Location);
        }

        context.Error(DiagnosticCodes.InvalidIdentityDetail, $"Unexpected '{body.Content}' under an identity detail - expected a file reference or inline code block", body.Location);
        context.SkipBlock(body.Indent);
        return null;
    }

    static void RejectBody(ParserContext context, SourceLine parent, string code)
    {
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(code, $"Unexpected '{child.Content}' under an identity source - refresh and caching are the runtime's business; a detail has exactly one source", child.Location);
            context.SkipBlock(child.Indent);
        }
    }

    [GeneratedRegex(@"^(@?[a-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)(?:\s+from\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex DetailRegex();

    [GeneratedRegex(@"^claim\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex ClaimRegex();

    [GeneratedRegex(@"^query\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)(?:\s+by\s+(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex QueryRegex();
}
