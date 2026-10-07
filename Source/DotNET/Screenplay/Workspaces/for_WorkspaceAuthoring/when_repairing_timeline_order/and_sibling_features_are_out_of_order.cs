// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_sibling_features_are_out_of_order : given.a_timeline
{
    [Fact]
    void should_move_the_producer_before_the_consumer_and_preserve_the_model()
    {
        var workspace = Features(("Consumer", [], ["E"]), ("Producer", ["E"], []), ("Last", [], []));
        var repair = Repair(workspace, "E");
        var move = (MoveWorkspaceNode)repair.Operations.Single();
        ((FeatureSyntax)move.Expected).Name.ShouldEqual("Producer");
        move.Index.ShouldEqual(0);
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
