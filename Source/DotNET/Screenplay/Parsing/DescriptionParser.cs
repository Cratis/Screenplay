// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Parsing;

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
    public static string? Parse(ParserContext context, SourceLine line, string? existing, string owner)
    {
        if (line.Content == "description")
        {
            return ParseFenced(context, line, existing, owner);
        }

        var match = DescriptionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid description '{line.Content}' - expected 'description \"<text>\"'", line.Location);
            return existing;
        }

        return Keep(context, line, existing, owner, match.Groups[1].Value);
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
            context.Error($"{owner} declares an empty description - the fenced block must contain text", line.Location);
            return existing;
        }

        return Keep(context, line, existing, owner, text);
    }

    static string? Keep(ParserContext context, SourceLine line, string? existing, string owner, string description)
    {
        if (existing is not null)
        {
            context.Error($"{owner} already declares a description - at most one is allowed", line.Location);
            return existing;
        }

        return description;
    }

    [GeneratedRegex(@"^description\s+""([^""]*)""$", RegexOptions.None, 1000)]
    private static partial Regex DescriptionRegex();
}
