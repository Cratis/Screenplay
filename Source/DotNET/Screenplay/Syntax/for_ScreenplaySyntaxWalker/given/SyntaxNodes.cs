// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.given;

/// <summary>
/// Enumerates a syntax tree by reflection rather than by the walker, so the walk can be held against an
/// answer the walker had no hand in producing.
/// </summary>
public static class SyntaxNodes
{
    public static IReadOnlyList<SyntaxNode> Under(SyntaxNode root)
    {
        var nodes = new List<SyntaxNode>();
        Collect(root, nodes);
        return nodes;
    }

    static void Collect(SyntaxNode node, List<SyntaxNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in ChildrenOf(node))
        {
            Collect(child, nodes);
        }
    }

    static IEnumerable<SyntaxNode> ChildrenOf(SyntaxNode node) =>
        node.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Select(property => property.GetValue(node))
            .SelectMany(Flatten);

    static IEnumerable<SyntaxNode> Flatten(object? value) => value switch
    {
        SyntaxNode node => [node],
        string => [],
        IEnumerable sequence => sequence.OfType<SyntaxNode>(),
        _ => []
    };
}
