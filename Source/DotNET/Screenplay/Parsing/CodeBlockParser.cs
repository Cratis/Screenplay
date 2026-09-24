// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses fenced inline code blocks with the language on the opening fence.
/// </summary>
internal static class CodeBlockParser
{
    /// <summary>
    /// Parses fenced code from a consumed opening fence or legacy language line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="tagLine">The consumed opening fence or legacy language line.</param>
    /// <returns>The parsed <see cref="CodeBlockSyntax"/>, or <c>null</c> when the fence is malformed.</returns>
    public static CodeBlockSyntax? Parse(ParserContext context, SourceLine tagLine)
    {
        var language = tagLine.Content.StartsWith("```", StringComparison.Ordinal)
            ? tagLine.Content[3..]
            : tagLine.Content;
        if (!context.Languages.InlineLanguages.Contains(language))
        {
            context.Error(DiagnosticCodes.ExpectedCodeFence, $"Expected a registered language on the opening fence, not '{tagLine.Content}'", tagLine.Location);
            return null;
        }

        if (!tagLine.Content.StartsWith("```", StringComparison.Ordinal))
        {
            context.Warning(DiagnosticCodes.LegacyInlineCodeFence, $"'{language}' on its own line is deprecated - use '```{language}' instead", tagLine.Location);
        }

        return ParseFencedBody(context, language, tagLine);
    }

    /// <summary>
    /// Whether this line opens an inline code block, including legacy language lines.
    /// </summary>
    /// <param name="context">The parser context with the registered languages.</param>
    /// <param name="line">The candidate line.</param>
    /// <returns>Whether it is a code-block directive.</returns>
    public static bool IsCodeLine(ParserContext context, SourceLine line) =>
        line.Content.StartsWith("```", StringComparison.Ordinal) || context.Languages.InlineLanguages.Contains(line.Content);

    /// <summary>
    /// Parses the fenced text following an already consumed tag line, dedented to the opening fence.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="opener">The keyword that opened the block, used in diagnostics.</param>
    /// <param name="tagLine">The consumed <see cref="SourceLine"/> holding the opening keyword.</param>
    /// <returns>The fenced lines joined with newlines, or <c>null</c> when the opening fence is missing.</returns>
    public static string? ParseFencedText(ParserContext context, string opener, SourceLine tagLine) =>
        ParseFencedBody(context, opener, tagLine)?.Code;

    /// <summary>Parses fenced text and preserves its exact original body positions.</summary>
    /// <param name="context">The parser context.</param>
    /// <param name="opener">The opening keyword or language.</param>
    /// <param name="tagLine">The consumed opening line.</param>
    /// <returns>The code block, or null when the opening fence is missing.</returns>
    public static CodeBlockSyntax? ParseFencedBody(ParserContext context, string opener, SourceLine tagLine)
    {
        var open = tagLine.Content.StartsWith("```", StringComparison.Ordinal) ? tagLine : context.Reader.PeekSignificant();
        var expectedFence = opener == "description" ? "```text" : $"```{opener}";
        if (open is null || (open != tagLine && open.Indent <= tagLine.Indent) ||
            (open.Content != expectedFence && !(open.Content == "```" && open != tagLine)))
        {
            context.Error(DiagnosticCodes.ExpectedCodeFence, $"Expected an opening ```{(opener == "description" ? "text" : opener)} fence after '{opener}'", tagLine.Location);
            return null;
        }

        if (open != tagLine)
        {
            context.Reader.TakeSignificant();
        }

        if (opener == "description" && open.Content == "```")
        {
            context.Warning(DiagnosticCodes.LegacyInlineCodeFence, "A bare description fence is deprecated - use '```text' instead", open.Location);
        }

        var code = new List<string>();
        var positions = new List<CodeBlockSourceLine>();
        SourceLine? first = null;
        SourceLine? last = null;
        SourceLine? closing = null;
        while (true)
        {
            var line = context.Reader.TakeRaw();
            if (line is null)
            {
                context.Error(DiagnosticCodes.UnclosedCodeBlock, "Unclosed inline code block - expected a closing ``` line", open.Location);
                break;
            }

            if (line.Raw.Trim() == "```")
            {
                closing = line;
                break;
            }

            first ??= line;
            var stripped = StripCount(line.Raw, open.Indent);
            code.Add(line.Raw[stripped..]);
            positions.Add(new(line.Number, stripped + 1));
            last = line;
        }

        var start = first?.Number ?? closing?.Number ?? open.Number;
        var startColumn = 1;
        var startOffset = closing?.StartOffset ?? (open.StartOffset + open.Raw.Length);
        if (first is not null)
        {
            startColumn = positions[0].Column;
            startOffset = first.StartOffset + startColumn - 1;
        }
        else if (closing is null)
        {
            startColumn = open.Raw.Length + 1;
        }

        return new CodeBlockSyntax(opener, string.Join('\n', code), tagLine.Location)
        {
            BodyStartOffset = startOffset,
            BodyEndOffset = last is null ? startOffset : last.StartOffset + last.Raw.Length,
            BodyStart = new(start, startColumn, tagLine.Path),
            BodyEnd = last is null ? new(start, startColumn, tagLine.Path) : new(last.Number, last.Raw.Length + 1, tagLine.Path),
            BodyLines = [.. positions]
        };
    }

    static int StripCount(string raw, int indent)
    {
        var strip = 0;
        while (strip < indent && strip < raw.Length && raw[strip] == ' ')
        {
            strip++;
        }

        return strip;
    }
}
