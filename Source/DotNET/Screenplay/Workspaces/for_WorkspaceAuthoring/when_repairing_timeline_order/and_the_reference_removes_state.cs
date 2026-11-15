// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_reference_removes_state : given.a_timeline
{
    [Fact]
    void should_identify_the_reference_node_and_offer_a_repair_that_preserves_the_model()
    {
        var consumer = Slice("Consumer", [], "E").Replace("    from E key id", "    remove with E key id", StringComparison.Ordinal);
        var workspace = Create(("root.play", "module M\n  feature F\n" + Indent(consumer, 4) + Indent(Slice("Producer", ["E"]), 4)));
        var repair = Repair(workspace, "E");
        WorkspaceSyntaxIndex.Create(workspace).Find(repair.Subject)!.Node.GetType().ShouldEqual(typeof(RemoveWithSyntax));
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
