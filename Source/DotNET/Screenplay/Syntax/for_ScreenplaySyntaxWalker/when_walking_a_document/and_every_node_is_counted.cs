// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_document;

public class and_every_node_is_counted : given.the_invoicing_document
{
    given.a_counting_walker _walker;
    IReadOnlyList<SyntaxNode> _expected;

    void Establish()
    {
        _walker = new();
        _expected = given.SyntaxNodes.Under(_document);
    }

    void Because() => _walker.VisitApplication(_document);

    [Fact] void should_reach_every_node_the_tree_holds() => _walker.Nodes.Count.ShouldEqual(_expected.Count);
    [Fact] void should_reach_the_root() => _walker.Nodes[0].ShouldEqual(_document);
    [Fact] void should_reach_a_node_of_every_kind_the_document_declares() => _walker.Nodes.Select(node => node.GetType()).Distinct().Count().ShouldEqual(_expected.Select(node => node.GetType()).Distinct().Count());
}
