// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Splits source text into <see cref="SourceLine">source lines</see> with indentation and comments resolved.
/// </summary>
internal static class SourceLineSplitter
{
    /// <summary>
    /// Splits the given source text into lines.
    /// </summary>
    /// <param name="source">The source text to split.</param>
    /// <param name="hashComments">Whether <c>#</c> starts a comment, in addition to <c>//</c>.</param>
    /// <returns>The <see cref="SourceLine">lines</see> of the source text.</returns>
    public static IReadOnlyList<SourceLine> Split(string source, bool hashComments = false)
    {
        var result = new List<SourceLine>();
        var number = 0;

        foreach (var raw in source.Split('\n'))
        {
            number++;
            var line = raw.TrimEnd('\r');
            var indent = line.Length - line.TrimStart().Length;
            var content = StripComments(line[indent..], hashComments).TrimEnd();
            result.Add(new(number, line, indent, content));
        }

        return result;
    }

    static string StripComments(string text, bool hashComments)
    {
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
            else if (!inString && !inTemplate)
            {
                if (current == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    return text[..i];
                }

                if (hashComments && current == '#')
                {
                    return text[..i];
                }
            }
        }

        return text;
    }
}
