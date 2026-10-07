// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_imports_follow_declarations : given.a_timeline
{
    [Fact]
    void should_refuse_import_hoisting_that_disagrees_with_the_simulated_ranks()
    {
        var workspace = Create(
            ("root.play", "module M\n  feature F\n" + Indent(Slice("Anchor", []), 4) + "    import \"a.play\"\n    import \"z.play\"\n"),
            ("a.play", Slice("Consumer", [], "E")),
            ("z.play", Slice("Producer", ["E"])));
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, Finding(workspace, "E")).ShouldBeEmpty();
    }
}
