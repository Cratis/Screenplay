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

        var code = ParseFencedText(context, language, tagLine);
        return code is null ? null : new CodeBlockSyntax(language, code, tagLine.Location);
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
    public static string? ParseFencedText(ParserContext context, string opener, SourceLine tagLine)
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
                break;
            }

            code.Add(Dedent(line.Raw, open.Indent));
        }

        return string.Join('\n', code);
    }

    static string Dedent(string raw, int indent)
    {
        var strip = 0;
        while (strip < indent && strip < raw.Length && raw[strip] == ' ')
        {
            strip++;
        }

        return raw[strip..];
    }
}
