// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.given;

/// <summary>
/// A walker that overrides nothing but <see cref="ScreenplaySyntaxWalker.VisitNode"/> - the shape a consumer
/// takes when it wants every node whatever its kind.
/// </summary>
public class a_counting_walker : ScreenplaySyntaxWalker
{
    public List<SyntaxNode> Nodes { get; } = [];

    public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
}
