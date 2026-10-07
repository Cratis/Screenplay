// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_moving_the_producer_creates_a_finding : given.a_timeline
{
    [Fact]
    void should_fall_back_to_moving_the_consumer_after_the_producer()
    {
        var workspace = Features(("Consumer", [], ["E"]), ("Middle", ["X"], []), ("Producer", ["E"], ["X"]));
        var repair = Repair(workspace, "E");
        var move = (MoveWorkspaceNode)repair.Operations.Single();
        ((FeatureSyntax)move.Expected).Name.ShouldEqual("Consumer");
        move.Index.ShouldEqual(3);
        Preserves(workspace, repair);
    }
}
