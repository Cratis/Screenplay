// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_producer_is_in_the_consumers_sub_feature : given.a_timeline
{
    [Fact]
    void should_refuse_the_finding()
    {
        var workspace = Create(("root.play", "module M\n  feature F\n" + Indent(Slice("Consumer", [], "E"), 4) + "    feature Sub\n" + Indent(Slice("Producer", ["E"]), 6)));
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, Finding(workspace, "E")).ShouldBeEmpty();
    }
}
