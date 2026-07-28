// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;

namespace Cratis.Screenplay.Text;

/// <summary>
/// Provides the escaping rules for the text carried by a Screenplay string literal.
/// </summary>
/// <remarks>
/// Escaping is what keeps printing and compiling inverses of each other - a value holding a <c>"</c>,
/// a <c>\</c> or a line break has to survive the trip out to <c>.play</c> text and back. The escape set is
/// deliberately small: <c>\\</c>, <c>\"</c>, <c>\n</c>, <c>\r</c> and <c>\t</c>. Any other backslash sequence
/// is left verbatim by <see cref="Unescape"/> so regular expression operands such as <c>matches "^\d+$"</c>
/// keep their meaning.
/// </remarks>
internal static class StringLiteral
{
    /// <summary>
    /// The regular expression fragment matching the body of a string literal - everything between the
    /// quotes, where an escaped quote does not terminate the literal.
    /// </summary>
    public const string BodyPattern = @"(?:[^""\\]|\\.)*";

    static readonly SearchValues<char> _needsEscaping = SearchValues.Create("\\\"\n\r\t");

    /// <summary>
    /// Renders a value as a quoted string literal, escaping the characters that would otherwise break it.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>The quoted and escaped literal text.</returns>
    public static string Quote(string value) => $"\"{Escape(value)}\"";

    /// <summary>
    /// Escapes the characters that cannot appear verbatim inside a string literal.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped text.</returns>
    public static string Escape(string value)
    {
        if (!NeedsEscaping(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append(@"\n");
                    break;
                case '\r':
                    builder.Append(@"\r");
                    break;
                case '\t':
                    builder.Append(@"\t");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Resolves the escape sequences of a string literal body back to the value they represent.
    /// </summary>
    /// <param name="value">The literal body to unescape.</param>
    /// <returns>The unescaped value.</returns>
    public static string Unescape(string value)
    {
        if (!value.Contains('\\', StringComparison.Ordinal))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\' || index + 1 == value.Length)
            {
                builder.Append(value[index]);
                continue;
            }

            switch (value[index + 1])
            {
                case '\\':
                    builder.Append('\\');
                    index++;
                    break;
                case '"':
                    builder.Append('"');
                    index++;
                    break;
                case 'n':
                    builder.Append('\n');
                    index++;
                    break;
                case 'r':
                    builder.Append('\r');
                    index++;
                    break;
                case 't':
                    builder.Append('\t');
                    index++;
                    break;
                default:
                    builder.Append('\\');
                    break;
            }
        }

        return builder.ToString();
    }

    static bool NeedsEscaping(string value) => value.AsSpan().ContainsAny(_needsEscaping);
}
