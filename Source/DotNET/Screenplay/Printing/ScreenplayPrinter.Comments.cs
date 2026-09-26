// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    // The writer records the actual instance as it writes each declaration. Source text is never used
    // to identify an owner: equal-looking siblings may move independently after an AST edit.
    static string PrintComments(SyntaxNode root, ScreenplayWriter writer)
    {
        var comments = new List<(SyntaxNode Owner, SourceComment Comment)>();
        Collect(root, comments);
        if (comments.Count == 0)
        {
            return writer.ToString();
        }

        var lines = writer.ToString().TrimEnd('\n').Split('\n');
        var before = new Dictionary<int, List<string>>();
        var after = new Dictionary<int, List<string>>();
        var trailing = new Dictionary<int, List<(string Text, bool Relocated)>>();
        var parents = new Dictionary<SyntaxNode, SyntaxNode>(ReferenceEqualityComparer.Instance);
        CollectParents(root, parents);
        foreach (var (owner, comment) in comments.OrderBy(entry => entry.Comment.Line))
        {
            var printedOwner = owner;
            while (!writer.Anchors.ContainsKey(printedOwner) && parents.TryGetValue(printedOwner, out var parent))
            {
                printedOwner = parent;
            }

            if (!writer.Anchors.TryGetValue(printedOwner, out var span))
            {
                continue;
            }

            var position = comment.Placement == SourceCommentPlacement.End ? span.Last : span.First;
            writer.DirectiveAnchors.TryGetValue(owner, out var directiveLines);
            if (owner.DirectiveLocations.Any(entry => DirectiveLocationKeys.IsCollectionKey(entry.Key) &&
                entry.Value.Line == comment.AnchorLine) &&
                directiveLines?.ContainsKey(comment.AnchorLine) != true)
            {
                // A removed collection value has no printed line. Never attach its comment to a new neighbor.
                continue;
            }

            if (directiveLines is not null && directiveLines.TryGetValue(comment.AnchorLine, out var directiveLine))
            {
                position = directiveLine;
            }
            else if (owner is ScreenTemplateSyntax { FitsSlotLocation: { } fitsSlotLocation } template &&
                comment.AnchorLine == fitsSlotLocation.Line && writer.FitsSlotAnchors.TryGetValue(template, out var fitsSlotLine))
            {
                position = fitsSlotLine;
            }
            else if (comment.Placement == SourceCommentPlacement.Trailing &&
                owner.DirectiveLocations.Any(entry => entry.Value.Line == comment.AnchorLine &&
                    entry.Key.StartsWith("automap previous:", StringComparison.Ordinal)) &&
                owner.DirectiveLocations.TryGetValue("automap", out var autoMapLocation) &&
                directiveLines?.TryGetValue(autoMapLocation.Line, out var printedAutoMapLine) == true)
            {
                position = printedAutoMapLine;
            }
            else if (comment.Placement == SourceCommentPlacement.Trailing &&
                !owner.DirectiveLocations.Values.Any(location => location.Line == comment.AnchorLine) &&
                owner.Location.Line > 0 && comment.Line > owner.Location.Line)
            {
                position = Math.Min(span.Last, span.First + comment.Line - owner.Location.Line);
            }

            position = Math.Clamp(position, 0, lines.Length - 1);
            var indent = lines[position].Length - lines[position].TrimStart().Length;
            if (comment.Placement == SourceCommentPlacement.End && owner != root)
            {
                indent = lines[span.First].Length - lines[span.First].TrimStart().Length + 2;
            }

            if (comment.Placement == SourceCommentPlacement.Trailing)
            {
                var relocated = comment.AnchorLine != owner.Location.Line &&
                    owner.DirectiveLocations.Values.Any(location => location.Line == comment.AnchorLine) &&
                    directiveLines?.ContainsKey(comment.AnchorLine) != true;
                var next = position + 1;
                if (relocated && next < lines.Length &&
                    lines[next].Length - lines[next].TrimStart().Length > indent)
                {
                    // A comment on an omitted directive belongs to this owner's body, not the
                    // previous line or an unrelated body line chosen by its old source offset.
                    if (!before.TryGetValue(next, out var leading))
                    {
                        before[next] = leading = [];
                    }

                    leading.Add(lines[next][..(lines[next].Length - lines[next].TrimStart().Length)] + comment.Text);
                }
                else
                {
                    if (!trailing.TryGetValue(position, out var side))
                    {
                        trailing[position] = side = [];
                    }

                    // Keep a comment on the printed directive inline; relocate the others.
                    side.Add((comment.Text, relocated));
                }
            }
            else
            {
                var destination = comment.Placement == SourceCommentPlacement.Leading ? before : after;
                if (!destination.TryGetValue(position, out var output))
                {
                    destination[position] = output = [];
                }

                output.Add(new string(' ', indent) + comment.Text);
            }
        }

        var result = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            if (before.TryGetValue(index, out var preceding))
            {
                result.AddRange(preceding);
            }

            if (trailing.TryGetValue(index, out var side))
            {
                var inline = side.FindIndex(entry => !entry.Relocated);
                if (inline < 0)
                {
                    inline = 0;
                }

                // Ordinary trailing comments sharing a printed line retain the original inline behavior.
                var inlineComments = side.Where((entry, commentIndex) => !entry.Relocated || commentIndex == inline).ToArray();
                result.Add(lines[index] + " " + string.Join(' ', inlineComments.Select(entry => entry.Text)));
                var indent = lines[index][..(lines[index].Length - lines[index].TrimStart().Length)];
                for (var commentIndex = 0; commentIndex < side.Count; commentIndex++)
                {
                    if (commentIndex != inline && side[commentIndex].Relocated)
                    {
                        result.Add(indent + side[commentIndex].Text);
                    }
                }
            }
            else
            {
                result.Add(lines[index]);
            }

            if (after.TryGetValue(index, out var following))
            {
                result.AddRange(following);
            }
        }

        return string.Join('\n', result) + '\n';
    }

    static void CollectParents(SyntaxNode node, Dictionary<SyntaxNode, SyntaxNode> parents)
    {
        var descriptor = SyntaxKinds.All.Single(kind => kind.Type == node.GetType());
        foreach (var member in descriptor.Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                parents[child] = node;
                CollectParents(child, parents);
            }
            else if (value is IEnumerable children and not string)
            {
                foreach (var item in children)
                {
                    if (item is SyntaxNode nested)
                    {
                        parents[nested] = node;
                        CollectParents(nested, parents);
                    }
                }
            }
        }
    }

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
