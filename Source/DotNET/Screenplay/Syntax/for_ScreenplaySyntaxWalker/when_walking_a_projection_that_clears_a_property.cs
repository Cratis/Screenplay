// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_a_projection_that_clears_a_property : Specification
{
    const string Source =
        """
        projection Notes => NoteReadModel
          from NoteCleared
            clear note
            summary = null
        """;

    walker _walker;
    Projections.ProjectionSyntax _projection;

    void Establish()
    {
        _walker = new();
        _projection = new ScreenplayCompiler().CompileProjection(Source).Value!;
    }

    void Because() => _walker.VisitProjection(_projection);

    [Fact] void should_reach_every_node_the_projection_holds() => _walker.Nodes.Count.ShouldEqual(given.SyntaxNodes.Under(_projection).Count);
    [Fact] void should_reach_the_clear_mapping_as_a_node() => _walker.Nodes.OfType<Projections.ClearMappingSyntax>().Count().ShouldEqual(1);
    [Fact] void should_dispatch_the_clear_to_its_own_method() => _walker.Cleared.ShouldContainOnly("note");
    [Fact] void should_keep_dispatching_the_assignment_to_its_own_method() => _walker.Assigned.ShouldContainOnly("summary");

    class walker : ScreenplaySyntaxWalker
    {
        public List<SyntaxNode> Nodes { get; } = [];

        public List<string> Cleared { get; } = [];

        public List<string> Assigned { get; } = [];

        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);

        public override void VisitClearMapping(Projections.ClearMappingSyntax syntax)
        {
            Cleared.Add(syntax.Property);
            base.VisitClearMapping(syntax);
        }

        public override void VisitSetMapping(Projections.SetMappingSyntax syntax)
        {
            Assigned.Add(syntax.Property);
            base.VisitSetMapping(syntax);
        }
    }
}
