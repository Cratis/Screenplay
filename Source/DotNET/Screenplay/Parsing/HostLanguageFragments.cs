// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Finds host-language operators and calls (<c>&amp;&amp;</c>, <c>||</c>, <c>Name(</c>) in raw expression text.
/// </summary>
/// <remarks>
/// Quoted strings, backtick templates and structured values are not scanned.
/// </remarks>
internal static class HostLanguageFragments
{
    static readonly HashSet<string> _wordOperators = new(StringComparer.Ordinal) { "and", "or", "not", "contains", "with", "of", "is", "in" };

    /// <summary>
    /// Finds the first host-language fragment in the text.
    /// </summary>
    /// <param name="text">The raw text to scan.</param>
    /// <returns>The fragment and its offset, or <c>null</c> when there is none.</returns>
    public static (string Fragment, int Offset)? Find(string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            var character = text[index];
            if (character is '"' or '\'' or '`')
            {
                index = SkipQuoted(text, index);
            }
            else if (character is '{' or '[')
            {
                index = SkipStructured(text, index);
            }
            else if ((character == '&' || character == '|') && index + 1 < text.Length && text[index + 1] == character)
            {
                return (new string(character, 2), index);
            }
            else if (IsIdentifierStart(character))
            {
                var end = index;
                while (end < text.Length && IsIdentifierPart(text[end])) end++;
                var word = text[index..end];
                if (end < text.Length && text[end] == '(' && !_wordOperators.Contains(word))
                {
                    return ($"{word}(", index);
                }

                index = end;
            }
            else
            {
                index++;
            }
        }

        return null;
    }

    /// <summary>
    /// Reports a <see cref="DiagnosticCodes.HostLanguageExpression"/> warning when the text carries a host-language fragment.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    /// <param name="text">The raw text to scan.</param>
    /// <param name="location">The <see cref="SourceLocation"/> to report at.</param>
    public static void Warn(ParserContext context, string text, SourceLocation location)
    {
        if (Find(text) is not { } found)
        {
            return;
        }

        var message = found.Fragment switch
        {
            "&&" => "Host-language '&&' is not Screenplay - use 'and'",
            "||" => "Host-language '||' is not Screenplay - use 'or'",
            _ => $"Host-language call '{found.Fragment}' is not Screenplay - move it into an inline code block or a 'rule'"
        };
        context.Warning(DiagnosticCodes.HostLanguageExpression, message, location);
    }

    static bool IsIdentifierStart(char character) => char.IsLetter(character) || character == '_';

    static bool IsIdentifierPart(char character) => char.IsLetterOrDigit(character) || character is '_' or '.' or '$';

    static int SkipQuoted(string text, int start)
    {
        var quote = text[start];
        for (var index = start + 1; index < text.Length; index++)
        {
            if (text[index] == '\\')
            {
                index++;
            }
            else if (text[index] == quote)
            {
                return index + 1;
            }
        }

        return text.Length;
    }

    static int SkipStructured(string text, int start)
    {
        var depth = 0;
        var index = start;
        while (index < text.Length)
        {
            var character = text[index];
            if (character is '"' or '\'')
            {
                index = SkipQuoted(text, index);
                continue;
            }

            if (character is '{' or '[') depth++;
            else if (character is '}' or ']') depth--;
            index++;
            if (depth == 0) break;
        }

        return index;
    }
}
