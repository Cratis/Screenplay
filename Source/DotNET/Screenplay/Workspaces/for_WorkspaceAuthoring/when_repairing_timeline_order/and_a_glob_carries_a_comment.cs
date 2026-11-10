// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_a_glob_carries_a_comment : given.a_timeline
{
    [Fact]
    void should_keep_each_comment_once_on_its_first_expanded_import()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"left/*.play\" // left files\n    import \"right/*.play\" // right files\n"),
            ("left/a.play", Slice("First", ["X"])),
            ("left/b.play", Slice("Consumer", [], "E")),
            ("right/y.play", Slice("Producer", ["E"], "X")),
            ("right/z.play", "// no timeline declaration\n"));
        var text = Propose(workspace, Repair(workspace, "E")).Workspace!.Documents.Single(document => document.Path.Value == "root.play").Text;
        text.ShouldEqual("module M\n\n  feature F\n    import \"left/a.play\" // left files\n    import \"right/y.play\" // right files\n    import \"left/b.play\"\n    import \"right/z.play\"\n");
    }
}
