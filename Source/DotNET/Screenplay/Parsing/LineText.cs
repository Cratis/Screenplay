// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Provides small text helpers shared by the parsers.
/// </summary>
internal static class LineText
{
    /// <summary>
    /// Gets the first whitespace separated word of a piece of content.
    /// </summary>
    /// <param name="content">The content to get the first word from.</param>
    /// <returns>The first word.</returns>
    public static string FirstWord(string content)
    {
        var space = content.IndexOf(' ', StringComparison.Ordinal);
        return space == -1 ? content : content[..space];
    }

    /// <summary>
    /// Removes the <c>@</c> escape prefix from identifiers, allowing keywords to be used as names.
    /// </summary>
    /// <param name="identifier">The identifier to unescape.</param>
    /// <returns>The unescaped identifier.</returns>
    public static string Unescape(string identifier) => identifier.Replace("@", string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// Splits text on a separator, ignoring separators inside string and template literals.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="separator">The separator character.</param>
    /// <returns>The split parts.</returns>
    public static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        var start = 0;
        var inString = false;
        var inTemplate = false;

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (current == '"' && !inTemplate)
            {
                inString = !inString;
            }
            else if (current == '`' && !inString)
            {
                inTemplate = !inTemplate;
            }
            else if (current == separator && !inString && !inTemplate)
            {
                parts.Add(text[start..i]);
                start = i + 1;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }
}
