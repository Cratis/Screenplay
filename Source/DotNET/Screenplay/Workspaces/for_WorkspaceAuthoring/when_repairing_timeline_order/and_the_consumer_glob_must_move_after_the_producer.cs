// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_consumer_glob_must_move_after_the_producer : given.a_timeline
{
    static ScreenplayWorkspace Model() => Create(
        ("root.play", "module M\n  feature F\n    import \"left/*.play\"\n    import \"right/*.play\"\n"),
        ("left/a.play", Slice("Consumer", [], "E")),
        ("left/b.play", Slice("Dependency", ["X"])),
        ("right/y.play", Slice("Producer", ["E"], "X")));

    [Fact]
    void should_expand_the_globs_with_the_consumer_after_the_producer()
    {
        var workspace = Model();
        var repair = Repair(workspace, "E");
        Propose(workspace, repair).Accepted.ShouldBeTrue();
        var feature = (FeatureSyntax)((ReplaceWorkspaceNode)repair.Operations.Single()).Node;
        feature.FileImports.Select(import => import.Pattern).ShouldEqual(["left/b.play", "right/y.play", "left/a.play"]);
        Preserves(workspace, repair);
    }
}
