// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Associates source comments with their nearest authored syntax owner without changing typed syntax.
/// </summary>
internal static class SourceCommentCapture
{
    internal static T Attach<T>(T root, IReadOnlyList<SourceLine> lines, bool hashComments = false)
        where T : SyntaxNode
    {
        var nodes = new List<SyntaxNode>();
        Visit(root, nodes);
        var comments = new Dictionary<SyntaxNode, ImmutableArray<SourceComment>.Builder>(ReferenceEqualityComparer.Instance);
        var inFence = false;
        foreach (var line in lines)
        {
            if (line.Content.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            var start = SourceLineSplitter.CommentStart(line.Raw[line.Indent..], hashComments);
            if (start < 0)
            {
                continue;
            }

            var text = line.Raw[(line.Indent + start)..];
            var trailing = line.Content.Length > 0;
            var next = lines.Skip(line.Number).FirstOrDefault(candidate => candidate.Content.Length > 0);
            var leading = !trailing && next is not null && line.Indent <= next.Indent;
            SourceLine? anchor = null;
            if (trailing)
            {
                anchor = line;
            }
            else if (leading)
            {
                anchor = next;
            }

            var owner = anchor is null ? Enclosing(nodes, line.Number, line.Indent, root) :
                nodes.Find(node => node.Location.Line == anchor.Number && node.Location.Column == anchor.Indent + 1)
                    ?? nodes.Find(node => node.DirectiveLocations.Values.Any(location => location.Line == anchor.Number && location.Column == anchor.Indent + 1))
                    ?? nodes.OfType<ScreenTemplateSyntax>().FirstOrDefault(template => template.FitsSlotLocation?.Line == anchor.Number && template.FitsSlotLocation?.Column == anchor.Indent + 1)
                    ?? Enclosing(nodes, anchor.Number, anchor.Indent, root);
            if (!comments.TryGetValue(owner, out var list))
            {
                comments[owner] = list = ImmutableArray.CreateBuilder<SourceComment>();
            }

            var placement = SourceCommentPlacement.End;
            if (trailing)
            {
                placement = SourceCommentPlacement.Trailing;
            }
            else if (leading)
            {
                placement = SourceCommentPlacement.Leading;
            }

            list.Add(new(
                line.Number,
                anchor?.Content ?? lines.ElementAtOrDefault(owner.Location.Line - 1)?.Content ?? string.Empty,
                text,
                placement)
            {
                OwnerAnchor = lines.ElementAtOrDefault(owner.Location.Line - 1)?.Content ?? string.Empty,
                AnchorLine = anchor?.Number ?? 0
            });
        }

        foreach (var (node, list) in comments)
        {
            typeof(SyntaxNode).GetProperty(nameof(SyntaxNode.SourceComments))!.SetValue(node, list.ToImmutable());
        }

        return root;
    }

    static SyntaxNode Enclosing(List<SyntaxNode> nodes, int line, int indent, SyntaxNode root) =>
        nodes.Where(node => node.Location.Line < line && node.Location.Column <= indent && node.Location.Line > 0)
            .OrderByDescending(node => node.Location.Line).ThenByDescending(node => node.Location.Column).FirstOrDefault() ?? root;

    static void Visit(SyntaxNode node, List<SyntaxNode> nodes)
    {
        nodes.Add(node);
        var descriptor = SyntaxKinds.All.Single(kind => kind.Type == node.GetType());
        foreach (var member in descriptor.Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                Visit(child, nodes);
            }
            else if (value is IEnumerable children and not string)
            {
                foreach (var item in children)
                {
                    if (item is SyntaxNode nested)
                    {
                        Visit(nested, nodes);
                    }
                }
            }
        }
    }
}
