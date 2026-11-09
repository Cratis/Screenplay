// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_a_projection_with_set_once_remove_and_ordering : Specification
{
    const string Source =
        """
        projection Models => ModelReadModel
          from SlotAssigned
            set once createdBy = $causedBy.subject
          from SlotDropped
            remove slots.$eventContent.slot
          children entries identified by entryId
            sort by addedAt descending
            from EntryAdded
              name = name
        """;

    walker _walker;
    Projections.ProjectionSyntax _projection;

    void Establish()
    {
        _walker = new();
        _projection = new ScreenplayCompiler().CompileProjection(Source).Value!;
    }

    void Because() => _walker.VisitProjection(_projection);

    [Fact] void should_reach_every_node_the_projection_holds() => _walker.Nodes.Count.ShouldEqual(global::Cratis.Screenplay.given.SyntaxNodes.Under(_projection).Count);
    [Fact] void should_dispatch_set_once_to_its_own_method() => _walker.SetOnce.ShouldContainOnly("createdBy");
    [Fact] void should_dispatch_remove_to_its_own_method() => _walker.Removed.ShouldContainOnly("slots.$eventContent.slot");
    [Fact] void should_visit_the_ordering() => _walker.SortedBy.ShouldContainOnly("addedAt");

    class walker : ScreenplaySyntaxWalker
    {
        public List<SyntaxNode> Nodes { get; } = [];

        public List<string> SetOnce { get; } = [];

        public List<string> Removed { get; } = [];

        public List<string> SortedBy { get; } = [];

        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);

        public override void VisitSetOnceMapping(Projections.SetOnceMappingSyntax syntax)
        {
            SetOnce.Add(syntax.Property);
            base.VisitSetOnceMapping(syntax);
        }

        public override void VisitRemoveKeyMapping(Projections.RemoveKeyMappingSyntax syntax)
        {
            Removed.Add(syntax.Property);
            base.VisitRemoveKeyMapping(syntax);
        }

        public override void VisitChildrenOrdering(Projections.ChildrenOrderingSyntax syntax)
        {
            SortedBy.Add(syntax.SortBy!);
            base.VisitChildrenOrdering(syntax);
        }
    }
}
