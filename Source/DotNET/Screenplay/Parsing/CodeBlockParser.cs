// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses fenced inline code blocks - a language tag line followed by code between <c>```</c> fences.
/// </summary>
internal static class CodeBlockParser
{
    /// <summary>
    /// Gets the language tags that open an inline code block.
    /// </summary>
    public static readonly IEnumerable<string> Languages = ["csharp", "typescript", "react", "html"];

    /// <summary>
    /// Parses the fenced code following an already consumed language tag line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="language">The language of the block.</param>
    /// <param name="tagLine">The consumed <see cref="SourceLine"/> holding the language tag.</param>
    /// <returns>The parsed <see cref="CodeBlockSyntax"/>, or <c>null</c> when the fence is malformed.</returns>
    public static CodeBlockSyntax? Parse(ParserContext context, string language, SourceLine tagLine)
    {
        var code = ParseFencedText(context, language, tagLine);
        return code is null ? null : new CodeBlockSyntax(language, code, tagLine.Location);
    }

    /// <summary>
    /// Parses the fenced text following an already consumed tag line, dedented to the opening fence.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="opener">The keyword that opened the block, used in diagnostics.</param>
    /// <param name="tagLine">The consumed <see cref="SourceLine"/> holding the opening keyword.</param>
    /// <returns>The fenced lines joined with newlines, or <c>null</c> when the opening fence is missing.</returns>
    public static string? ParseFencedText(ParserContext context, string opener, SourceLine tagLine)
    {
        var open = context.Reader.PeekSignificant();
        if (open is null || open.Content != "```" || open.Indent <= tagLine.Indent)
        {
            context.Error($"Expected an opening ``` fence after '{opener}'", tagLine.Location);
            return null;
        }

        context.Reader.TakeSignificant();

        var code = new List<string>();
        while (true)
        {
            var line = context.Reader.TakeRaw();
            if (line is null)
            {
                context.Error("Unclosed inline code block - expected a closing ``` line", open.Location);
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
