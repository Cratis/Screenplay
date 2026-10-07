// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_first_reference_is_a_join : given.a_timeline
{
    [Fact]
    void should_discover_and_propose_a_repair_for_the_backward_join_reference()
    {
        var consumer = Slice("Consumer", [], "E").Replace("    from E key id", "    join enrichment on id\n      with E\n    from E key id", StringComparison.Ordinal);
        var workspace = Create(("root.play", "module M\n  feature F\n" + Indent(consumer, 4) + Indent(Slice("Producer", ["E"]), 4)));
        WorkspaceTimelineRepairs.Timeline(workspace).SourceValid.ShouldBeTrue();
        var finding = Finding(workspace, "E");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Entries.Single(entry => entry.Node is JoinEventSyntax && entry.Location == finding.Location).ShouldNotBeNull();
        var repair = Repair(workspace, "E");
        ((JoinEventSyntax)index.Find(repair.Subject)!.Node).Event.ShouldEqual("E");
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
