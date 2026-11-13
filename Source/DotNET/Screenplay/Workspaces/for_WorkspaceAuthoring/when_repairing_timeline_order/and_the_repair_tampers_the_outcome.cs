// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_repair_tampers_the_outcome : given.a_timeline
{
    [Fact]
    void should_refuse_an_outcome_that_does_not_reproduce_the_planned_order()
    {
        var workspace = Features(("Consumer", [], ["E"]), ("Producer", ["E"], []), ("Last", [], []));
        var repair = Repair(workspace, "E");
        var result = Propose(workspace, repair);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        WorkspaceTimelineRepairs.KeepsTimeline(index, repair, result).ShouldBeTrue();

        var tampered = Features(("Consumer", [], ["E"]), ("Last", [], []), ("Producer", ["E"], []));
        WorkspaceTimelineRepairs.KeepsTimeline(index, repair, result with { Workspace = tampered }).ShouldBeFalse();
    }
}
