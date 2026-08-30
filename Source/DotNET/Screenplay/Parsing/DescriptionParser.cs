// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Represents the exact location and raw escaped length of a parsed single-line quoted description body.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the first character of the raw quoted body.</param>
/// <param name="RawLength">The raw escaped body length, in UTF-16 code units, exactly as it appears in the source text.</param>
internal readonly record struct DescriptionSpan(SourceLocation Location, int RawLength);

/// <summary>
/// Parses <c>description</c> declarations - the optional human readable description of a declaration,
/// as a single quoted line or a fenced multi-line block.
/// </summary>
internal static partial class DescriptionParser
{
    /// <summary>
    /// Parses a description from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>description</c> line.</param>
    /// <param name="existing">The description already parsed for the owning declaration, when there is one.</param>
    /// <param name="owner">The owning declaration, used in diagnostics.</param>
    /// <returns>The parsed description, or <paramref name="existing"/> when the line is invalid or a duplicate.</returns>
    public static string? Parse(ParserContext context, SourceLine line, string? existing, string owner) =>
        Parse(context, line, existing, owner, out _);

    /// <summary>
    /// Parses a description from its already consumed line, additionally reporting the exact location and raw
    /// escaped length of a newly accepted single-line quoted body.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>description</c> line.</param>
    /// <param name="existing">The description already parsed for the owning declaration, when there is one.</param>
    /// <param name="owner">The owning declaration, used in diagnostics.</param>
    /// <param name="span">The exact source span of the raw quoted body when a new single-line description was accepted; otherwise <c>null</c>.</param>
    /// <returns>The parsed description, or <paramref name="existing"/> when the line is invalid or a duplicate.</returns>
    internal static string? Parse(ParserContext context, SourceLine line, string? existing, string owner, out DescriptionSpan? span)
    {
        span = null;
        if (line.Content == "description")
        {
            return ParseFenced(context, line, existing, owner);
        }

        var match = DescriptionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDescription, $"Invalid description '{line.Content}' - expected 'description \"<text>\"'", line.Location);
            return existing;
        }

        var body = match.Groups[1];
        var description = Keep(context, line, existing, owner, StringLiteral.Unescape(body.Value), out var applied);
        if (applied)
        {
            span = new DescriptionSpan(line.LocationAt(body.Index), body.Length);
        }

        return description;
    }

    static string? ParseFenced(ParserContext context, SourceLine line, string? existing, string owner)
    {
        var text = CodeBlockParser.ParseFencedText(context, "description", line);
        if (text is null)
        {
            return existing;
        }

        if (text.Trim().Length == 0)
        {
            context.Error(DiagnosticCodes.EmptyDescription, $"{owner} declares an empty description - the fenced block must contain text", line.Location);
            return existing;
        }

        return Keep(context, line, existing, owner, text);
    }

    static string? Keep(ParserContext context, SourceLine line, string? existing, string owner, string description) =>
        Keep(context, line, existing, owner, description, out _);

    static string? Keep(ParserContext context, SourceLine line, string? existing, string owner, string description, out bool applied)
    {
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateDescription, $"{owner} already declares a description - at most one is allowed", line.Location);
            applied = false;
            return existing;
        }

        applied = true;
        return description;
    }

    [GeneratedRegex(@"^description\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex DescriptionRegex();
}
