// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    // Comments are bound to syntax owners, not to document offsets. The latter are no longer meaningful
    // after a declaration moves to its own file or another declaration is canonically printed.
    static string PrintComments(SyntaxNode root, string text)
    {
        var comments = new List<(SyntaxNode Owner, SourceComment Comment)>();
        Collect(root, comments);
        if (comments.Count == 0)
        {
            return text;
        }

        var lines = text.TrimEnd('\n').Split('\n').ToList();
        var before = new Dictionary<int, List<string>>();
        var after = new Dictionary<int, List<string>>();
        var trailing = new Dictionary<int, List<string>>();
        var claimed = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in comments.GroupBy(entry => (entry.Owner, entry.Comment.Anchor, entry.Comment.Placement))
                     .OrderBy(group => group.Min(entry => entry.Comment.Line)))
        {
            var (owner, anchor, placement) = group.Key;
            List<int> matches = [.. Enumerable.Range(0, lines.Count)
                .Where(index => lines[index].TrimStart() == anchor)];
            if (matches.Count == 0)
            {
                matches = [.. Enumerable.Range(0, lines.Count)
                    .Where(index => Matches(lines[index].TrimStart(), anchor))];
            }

            var ownerAnchor = group.First().Comment.OwnerAnchor;
            if (owner != root && ownerAnchor.Length > 0)
            {
                var headers = Enumerable.Range(0, lines.Count)
                    .Where(index => lines[index].TrimStart() == ownerAnchor)
                    .OrderBy(index => Math.Abs(Indentation(lines[index]) - owner.Location.Column + 1))
                    .ToList();
                if (headers.Count > 0)
                {
                    var header = headers[0];
                    var depth = Indentation(lines[header]);
                    var boundary = header + 1;
                    while (boundary < lines.Count && (lines[boundary].Length == 0 || Indentation(lines[boundary]) > depth))
                    {
                        boundary++;
                    }

                    var inOwner = matches.Where(index => index >= header && index < boundary).ToList();
                    if (inOwner.Count > 0)
                    {
                        matches = inOwner;
                    }
                }
            }
            var key = $"{owner.Location.Path}:{anchor}:{placement}";
            var occurrence = claimed.GetValueOrDefault(key);
            claimed[key] = occurrence + 1;
            var position = matches.Count > 0 ? matches[Math.Min(occurrence, matches.Count - 1)] : -1;
            if (position < 0)
            {
                // A typed edit may have changed the anchor's content. Use the owning declaration's
                // header before falling back to the document boundary; never silently lose the comment.
                position = lines.FindIndex(line => line.TrimStart().StartsWith(anchor.Split('=')[0].TrimEnd(), StringComparison.Ordinal));
                if (position < 0)
                {
                    position = Math.Max(0, lines.Count - 1);
                }
            }

            var indent = lines[position].Length - lines[position].TrimStart().Length;
            if (placement == SourceCommentPlacement.End)
            {
                var end = position;
                while (end + 1 < lines.Count && (lines[end + 1].Length == 0 || lines[end + 1].TakeWhile(char.IsWhiteSpace).Count() > indent))
                {
                    end++;
                }

                position = end;
                indent += owner == root ? 0 : 2;
            }

            var destination = placement switch
            {
                SourceCommentPlacement.Leading => before,
                SourceCommentPlacement.Trailing => trailing,
                _ => after
            };
            if (!destination.TryGetValue(position, out var output))
            {
                destination[position] = output = [];
            }

            output.AddRange(group.OrderBy(entry => entry.Comment.Line).Select(entry =>
                placement == SourceCommentPlacement.Trailing ? entry.Comment.Text : new string(' ', indent) + entry.Comment.Text));
        }

        var result = new List<string>();
        for (var index = 0; index < lines.Count; index++)
        {
            if (before.TryGetValue(index, out var preceding))
            {
                result.AddRange(preceding);
            }

            result.Add(lines[index] + (trailing.TryGetValue(index, out var side) ? " " + string.Join(' ', side) : string.Empty));
            if (after.TryGetValue(index, out var following))
            {
                result.AddRange(following);
            }
        }

        return string.Join('\n', result) + '\n';
    }

    static int Indentation(string line) => line.Length - line.TrimStart().Length;

    static bool Matches(string printed, string source) =>
        printed == source || (source.Contains(" = ", StringComparison.Ordinal) &&
            printed.StartsWith(source[..source.IndexOf(" = ", StringComparison.Ordinal)] + " = ", StringComparison.Ordinal));

    static void Collect(SyntaxNode node, List<(SyntaxNode Owner, SourceComment Comment)> comments)
    {
        comments.AddRange(node.SourceComments.Select(comment => (node, comment)));
        var descriptor = SyntaxKinds.All.Single(kind => kind.Type == node.GetType());
        foreach (var member in descriptor.Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                Collect(child, comments);
            }
            else if (value is IEnumerable children and not string)
            {
                foreach (var item in children)
                {
                    if (item is SyntaxNode nested)
                    {
                        Collect(nested, comments);
                    }
                }
            }
        }
    }
}
