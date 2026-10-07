// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_imports_follow_declarations : given.a_timeline
{
    [Fact]
    void should_move_the_import_without_hoisting_it_before_the_declaration()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n" + Indent(Slice("Anchor", []), 4) + "    import \"a.play\"\n    import \"z.play\"\n"),
            ("a.play", Slice("Consumer", [], "E")),
            ("z.play", Slice("Producer", ["E"])));
        var repair = Repair(workspace, "E");
        Preserves(workspace, repair);
        var text = Propose(workspace, repair).Workspace!.Documents.Single(document => document.Path.Value == "root.play").Text;
        text.IndexOf("slice StateView Anchor", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("import \"z.play\"", StringComparison.Ordinal));
        text.IndexOf("import \"z.play\"", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("import \"a.play\"", StringComparison.Ordinal));
    }
}
