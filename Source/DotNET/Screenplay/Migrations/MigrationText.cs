// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Writes the edits of a migration step into the text the step read: each edit prints nodes of the rewritten tree with
/// the printer, and every line no edit touches is kept byte for byte.
/// </summary>
internal static class MigrationText
{
    /// <summary>
    /// Applies edits to a source text.
    /// </summary>
    /// <param name="source">The text the step read.</param>
    /// <param name="edits">The <see cref="MigrationEdit">edits</see>, in the order their insertions should appear.</param>
    /// <param name="printer">The <see cref="ScreenplayPrinter"/> printing the nodes.</param>
    /// <returns>The rewritten text.</returns>
    /// <exception cref="InvalidMigrationEdit">Two edits touch the same line, or an edit names a line the text does not have.</exception>
    public static string Apply(string source, IEnumerable<MigrationEdit> edits, ScreenplayPrinter printer)
    {
        var newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = SourceLineSplitter.Split(source).Select(line => line.Raw).ToArray();
        var replaced = new Dictionary<int, IReadOnlyList<string>>();
        var removed = new HashSet<int>();
        var appended = new Dictionary<int, List<string>>();
        var claimed = new HashSet<int>();

        void Claim(int line)
        {
            if (line < 1 || line > lines.Length)
            {
                throw new InvalidMigrationEdit($"An edit names line {line}, but the document has {lines.Length} lines");
            }

            if (!claimed.Add(line))
            {
                throw new InvalidMigrationEdit($"Two edits change line {line}");
            }
        }

        void Append(int afterLine, IEnumerable<string> text)
        {
            if (!appended.TryGetValue(afterLine, out var list))
            {
                appended[afterLine] = list = [];
            }

            list.AddRange(text);
        }

        foreach (var edit in edits)
        {
            switch (edit)
            {
                case ReplaceLine replace:
                    Claim(replace.Line);
                    var indent = IndentText(lines[replace.Line - 1]);
                    replaced[replace.Line] = [.. replace.Nodes.Select((node, index) => indent + FirstLine(printer, node) + (index == 0 ? TrailingComment(lines[replace.Line - 1]) : string.Empty))];
                    break;

                case InsertAfterBlock insert:
                    Exists(insert.Line);
                    var insertIndent = IndentText(lines[insert.Line - 1]);
                    Append(BlockEnd(lines, insert.Line), printer.PrintFragment(insert.Node).Select(line => insertIndent + line));
                    break;

                case MoveBlock move:
                    Exists(move.After);
                    var start = CommentsAbove(lines, move.Line);
                    var end = BlockEnd(lines, move.Line);
                    var target = Indent(lines[move.After - 1]) + Indent(lines[move.Line - 1]) - Indent(lines[Parent(lines, move.Line) - 1]);
                    var delta = target - Indent(lines[move.Line - 1]);
                    var moved = new List<string>();
                    for (var line = start; line <= end; line++)
                    {
                        Claim(line);
                        removed.Add(line);
                        moved.Add(line == move.Line
                            ? new string(' ', target) + FirstLine(printer, move.Node) + TrailingComment(lines[line - 1])
                            : Reindent(lines[line - 1], delta));
                    }

                    Append(BlockEnd(lines, move.After), moved);
                    break;

                case RemoveLine remove:
                    Claim(remove.Line);
                    removed.Add(remove.Line);
                    break;

                default:
                    throw new InvalidMigrationEdit($"Unknown edit {edit.GetType().Name}");
            }
        }

        var output = new List<string>(lines.Length);
        for (var line = 1; line <= lines.Length; line++)
        {
            if (!removed.Contains(line))
            {
                output.AddRange(replaced.TryGetValue(line, out var replacement) ? replacement : [lines[line - 1]]);
            }

            if (appended.TryGetValue(line, out var after))
            {
                output.AddRange(after);
            }
        }

        return string.Join(newline, output);

        void Exists(int line)
        {
            if (line < 1 || line > lines.Length)
            {
                throw new InvalidMigrationEdit($"An edit names line {line}, but the document has {lines.Length} lines");
            }
        }
    }

    /// <summary>
    /// Finds the last line of the block that starts at a line - the line and every deeper indented line after it.
    /// </summary>
    /// <param name="lines">The raw lines.</param>
    /// <param name="line">The 1-based line the block starts at.</param>
    /// <returns>The 1-based last line of the block.</returns>
    internal static int BlockEnd(string[] lines, int line)
    {
        var indent = Indent(lines[line - 1]);
        var last = line;
        for (var next = line + 1; next <= lines.Length; next++)
        {
            var raw = lines[next - 1];
            if (raw.Trim().Length == 0)
            {
                continue;
            }

            if (Indent(raw) <= indent)
            {
                break;
            }

            last = next;
        }

        return last;
    }

    static int Parent(string[] lines, int line)
    {
        var indent = Indent(lines[line - 1]);
        for (var previous = line - 1; previous >= 1; previous--)
        {
            var raw = lines[previous - 1];
            if (raw.Trim().Length > 0 && !IsComment(raw) && Indent(raw) < indent)
            {
                return previous;
            }
        }

        return line;
    }

    static int CommentsAbove(string[] lines, int line)
    {
        var indent = Indent(lines[line - 1]);
        var first = line;
        while (first > 1 && IsComment(lines[first - 2]) && Indent(lines[first - 2]) == indent)
        {
            first--;
        }

        return first;
    }

    static string FirstLine(ScreenplayPrinter printer, Syntax.SyntaxNode node) => printer.PrintFragment(node)[0];

    static bool IsComment(string raw) => raw.TrimStart().StartsWith("//", StringComparison.Ordinal);

    static int Indent(string raw) => raw.Length - raw.TrimStart().Length;

    static string IndentText(string raw) => raw[..Indent(raw)];

    static string Reindent(string raw, int delta)
    {
        if (raw.Trim().Length == 0 || delta == 0)
        {
            return raw;
        }

        return delta > 0 ? new string(' ', delta) + raw : raw[Math.Min(-delta, Indent(raw))..];
    }

    // The comment written after a line's content, with the whitespace before it, so a rewritten line keeps it.
    static string TrailingComment(string raw)
    {
        var indent = Indent(raw);
        var start = SourceLineSplitter.CommentStart(raw[indent..]);
        if (start < 0)
        {
            return string.Empty;
        }

        var content = raw[indent..(indent + start)].TrimEnd();
        return content.Length == 0 ? string.Empty : raw[(indent + content.Length)..];
    }
}
