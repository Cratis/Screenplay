// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_first_reference_is_a_remove : given.a_timeline
{
    [Fact]
    void should_repair_the_removal_and_establish_the_document_identity()
    {
        var consumer = Slice("Consumer", [], "E").Replace("from E key id", "remove with E key id", StringComparison.Ordinal)
            + "  projection Later\n    from E key id\n";
        var workspace = Create(("root.play", "module M\n  feature F\n" + Indent(consumer, 4) + Indent(Slice("Producer", ["E"]), 4)));
        WorkspaceTimelineRepairs.Timeline(workspace).SourceValid.ShouldBeTrue();
        var finding = Finding(workspace, "E");
        var repair = Repair(workspace, "E");
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var subject = index.Find(repair.Subject)!;
        repair.Subject.ShouldEqual(index.Entries.Single(entry => entry.Node is RemoveWithSyntax && entry.Location == finding.Location).Handle);
        ((SliceSyntax)((MoveWorkspaceNode)repair.Operations.Single()).Expected).Name.ShouldEqual("Producer");
        subject.Node.ShouldBeOfExactType<RemoveWithSyntax>();
        subject.Location.ShouldEqual(finding.Location);
        var result = Propose(workspace, repair);
        result.Accepted.ShouldBeTrue();
        result.Conflicts.ShouldBeEmpty();
        var proposed = result.Workspace!;
        workspace.IdentityCatalog.Documents.ShouldBeEmpty();
        var originalDocument = workspace.Documents.Single();
        var expectedCatalog = SemanticIdentityCatalog.Create(workspace.IdentityCatalog.Application,
            [new(originalDocument.StableKey, originalDocument.Id, SemanticIdentityOrigin.Persisted)], workspace.IdentityCatalog.Semantics, workspace.IdentityCatalog.EventContracts);
        proposed.IdentityCatalog.Revision.ShouldEqual(expectedCatalog.Revision);
        proposed.IdentityCatalog.Revision.ShouldNotEqual(workspace.IdentityCatalog.Revision);
        WorkspaceTimelineRepairs.KeepsTimeline(index, repair, result).ShouldBeTrue();
        proposed.Documents.Select(document => document.Path).ShouldEqual(workspace.Documents.Select(document => document.Path));
        result.ExecutableReady.ShouldEqual(workspace.Compilation.Success);
        repair.CanFixAll.ShouldBeTrue();
        proposed.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
        WorkspaceSyntaxIndex.Create(proposed).Entries.Where(entry => entry.Node is SliceSyntax).Select(entry => ((SliceSyntax)entry.Node).Name).ShouldEqual(["Producer", "Consumer"]);
    }
}
