// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_sibling_imports_are_out_of_order : given.a_timeline
{
    [Fact]
    void should_move_a_file_import_and_preserve_the_model()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"consumer.play\"\n    import \"producer.play\"\n"),
            ("consumer.play", Slice("Consumer", [], "E")),
            ("producer.play", Slice("Producer", ["E"])));
        var repair = Repair(workspace, "E");
        ((MoveWorkspaceNode)repair.Operations.Single()).Member.ShouldEqual("fileImports");
        Preserves(workspace, repair);
    }
}
