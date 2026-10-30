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
            if (SkipOpaque(text, index) is { } opaqueEnd)
            {
                index = opaqueEnd;
                continue;
            }

            if (DoubledOperatorAt(text, index) is { } operatorFragment)
            {
                return operatorFragment;
            }

            var end = IdentifierEnd(text, index);
            if (end == index)
            {
                index++;
                continue;
            }

            if (CallAt(text, index, end) is { } call)
            {
                return call;
            }

            index = end;
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

        context.Warning(DiagnosticCodes.HostLanguageExpression, MessageFor(found.Fragment), location);
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

    static string MessageFor(string fragment) => fragment switch
    {
        "&&" => "Host-language '&&' is not Screenplay - use 'and'",
        "||" => "Host-language '||' is not Screenplay - use 'or'",
        _ => $"Host-language call '{fragment}' is not Screenplay - move it into an inline code block or a 'rule'"
    };

    static bool IsStringQuote(char character) => character is '"' or '\'';

    static int DepthChange(char character) => character switch
    {
        '{' or '[' => 1,
        '}' or ']' => -1,
        _ => 0
    };

    static int? SkipOpaque(string text, int index)
    {
        if (IsStringQuote(text[index]) || text[index] == '`') return SkipQuoted(text, index);
        if (DepthChange(text[index]) > 0) return SkipStructured(text, index);
        return null;
    }

    static (string Fragment, int Offset)? DoubledOperatorAt(string text, int index)
    {
        var character = text[index];
        var isOperatorCharacter = character is '&' or '|';
        return isOperatorCharacter && index + 1 < text.Length && text[index + 1] == character ? (new string(character, 2), index) : null;
    }

    static int IdentifierEnd(string text, int start)
    {
        if (!IsIdentifierStart(text[start])) return start;
        var end = start;
        while (end < text.Length && IsIdentifierPart(text[end])) end++;
        return end;
    }

    static (string Fragment, int Offset)? CallAt(string text, int start, int end)
    {
        var word = text[start..end];
        return end < text.Length && text[end] == '(' && !_wordOperators.Contains(word) ? ($"{word}(", start) : null;
    }

    static int SkipStructured(string text, int start)
    {
        var depth = 0;
        var index = start;
        do
        {
            if (IsStringQuote(text[index]))
            {
                index = SkipQuoted(text, index);
            }
            else
            {
                depth += DepthChange(text[index]);
                index++;
            }
        }
        while (depth > 0 && index < text.Length);

        return index;
    }
}
