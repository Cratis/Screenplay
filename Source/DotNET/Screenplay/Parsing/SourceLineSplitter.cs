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
    /// <param name="path">The path of the file the source text came from, stamped onto every line.</param>
    /// <returns>The <see cref="SourceLine">lines</see> of the source text.</returns>
    public static IReadOnlyList<SourceLine> Split(string source, bool hashComments = false, string? path = null)
    {
        var result = new List<SourceLine>();
        var number = 0;

        foreach (var raw in source.Split('\n'))
        {
            number++;
            var line = raw.TrimEnd('\r');
            var indent = line.Length - line.TrimStart().Length;
            var content = StripComments(line[indent..], hashComments).TrimEnd();
            result.Add(new(number, line, indent, content, path));
        }

        return result;
    }

    internal static int CommentStart(string text, bool hashComments = false)
    {
        var inString = false;
        var inTemplate = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current == '\\' && inString && index + 1 < text.Length)
            {
                index++;
            }
            else if (current == '"' && !inTemplate)
            {
                inString = !inString;
            }
            else if (current == '`' && !inString)
            {
                inTemplate = !inTemplate;
            }
            else if (!inString && !inTemplate)
            {
                if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
                {
                    return index;
                }

                if (hashComments && current == '#')
                {
                    return index;
                }
            }
        }

        return -1;
    }

    static string StripComments(string text, bool hashComments)
    {
        var comment = CommentStart(text, hashComments);
        return comment < 0 ? text : text[..comment];
    }
}
