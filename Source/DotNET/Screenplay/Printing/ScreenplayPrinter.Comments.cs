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
        var trailing = new Dictionary<int, List<string>>();
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
            if (writer.DirectiveAnchors.TryGetValue(owner, out var directiveLines) &&
                directiveLines.TryGetValue(comment.AnchorLine, out var directiveLine))
            {
                position = directiveLine;
            }
            else if (owner is ScreenTemplateSyntax { FitsSlotLocation: { } fitsSlotLocation } template &&
                comment.AnchorLine == fitsSlotLocation.Line && writer.FitsSlotAnchors.TryGetValue(template, out var fitsSlotLine))
            {
                position = fitsSlotLine;
            }
            else if (comment.Placement == SourceCommentPlacement.Trailing && owner.Location.Line > 0 && comment.Line > owner.Location.Line)
            {
                position = Math.Min(span.Last, span.First + comment.Line - owner.Location.Line);
            }

            position = Math.Clamp(position, 0, lines.Length - 1);
            var indent = lines[position].Length - lines[position].TrimStart().Length;
            if (comment.Placement == SourceCommentPlacement.End && owner != root)
            {
                indent = lines[span.First].Length - lines[span.First].TrimStart().Length + 2;
            }

            var destination = comment.Placement switch
            {
                SourceCommentPlacement.Leading => before,
                SourceCommentPlacement.Trailing => trailing,
                _ => after
            };
            if (!destination.TryGetValue(position, out var output))
            {
                destination[position] = output = [];
            }

            output.Add(comment.Placement == SourceCommentPlacement.Trailing ? comment.Text : new string(' ', indent) + comment.Text);
        }

        var result = new List<string>();
        for (var index = 0; index < lines.Length; index++)
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
