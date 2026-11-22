// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_first_reference_is_a_clear : given.a_timeline
{
    [Fact]
    void should_repair_the_nested_clearing_before_the_later_projection_reference()
    {
        var consumer = Slice("Consumer", [], "E")
            .Replace("    id Uuid\n", "    id Uuid\n    detail Detail optional\n", StringComparison.Ordinal)
            .Replace("from E key id", "nested detail\n      from Seed\n      clear with E\n    from E key id", StringComparison.Ordinal);
        var workspace = Create(("root.play", "type Detail\n  id Uuid\nmodule M\n  feature F\n" + Indent(consumer, 4) + Indent(Slice("Producer", ["E", "Seed"]), 4)));
        WorkspaceTimelineRepairs.Timeline(workspace).SourceValid.ShouldBeTrue();
        workspace.Compilation.Success.ShouldBeTrue();
        workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
        var finding = Finding(workspace, "E");
        var repair = Repair(workspace, "E");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var subject = index.Find(repair.Subject)!;
        repair.Subject.ShouldEqual(index.Entries.Single(entry => entry.Node is ClearWithSyntax && entry.Location == finding.Location).Handle);
        ((SliceSyntax)((MoveWorkspaceNode)repair.Operations.Single()).Expected).Name.ShouldEqual("Producer");
        subject.Node.ShouldBeOfExactType<ClearWithSyntax>();
        subject.Location.ShouldEqual(finding.Location);
        Preserves(workspace, repair);
        var proposed = Propose(workspace, repair).Workspace!;
        proposed.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
        WorkspaceSyntaxIndex.Create(proposed).Entries.Where(entry => entry.Node is SliceSyntax).Select(entry => ((SliceSyntax)entry.Node).Name).ShouldEqual(["Producer", "Consumer"]);
    }
}
