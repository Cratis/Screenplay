// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_reference_is_reads_or_a_reducer : given.a_timeline
{
    [Theory]
    [InlineData("slice StateChange Consumer\n  command C\n    reads Items\n", "slice StateView Producer\n  readmodel Items\n", typeof(ReadsSyntax))]
    [InlineData("slice StateView Consumer\n  readmodel Items\n  reducer R => Items\n    on E\n", "slice StateChange Producer\n  event E\n", typeof(ReducerRuleSyntax))]
    void should_offer_a_typed_move_and_preserve_the_model(string consumer, string producer, Type subjectType)
    {
        var original = Create(("root.play", "module M\n  feature F\n" + Indent(consumer, 4) + Indent(producer, 4)));
        var document = original.Documents.Single();
        var catalog = SemanticIdentityCatalog.Create(ApplicationIdentity.Create("Timeline"), [new(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)], [], []);
        var workspace = ScreenplayWorkspace.Create("Timeline", [document], catalog);
        var diagnostic = workspace.Compilation.Diagnostics.Single(value => value.Code == DiagnosticCodes.EventFromLaterSlice);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var repairs = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic);
        if (repairs.Length != 1)
        {
            var unverified = WorkspaceTimelineRepairs.Find(index, workspace.Revision, diagnostic, false);
            var preview = unverified.Length == 1 ? Propose(workspace, unverified[0]) : null;
            Xunit.Assert.Fail($"Expected one repair; unverified={unverified.Length}; conflicts={string.Join(';', preview?.Conflicts.Select(conflict => conflict.Message) ?? [])}; baseline={string.Join(';', workspace.Compilation.Diagnostics.Select(value => value.Code + ':' + value.Message))}");
        }
        var repair = repairs.Single();
        index.Find(repair.Subject)!.Node.GetType().ShouldEqual(subjectType);
        repair.Operations.Single().ShouldBeOfExactType<MoveWorkspaceNode>();
        Preserves(workspace, repair);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(value => value.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }

    [Fact]
    void should_offer_a_reads_move_in_a_fresh_workspace_and_assign_only_the_existing_document_identity()
    {
        var workspace = Create(("root.play", "module M\n  feature F\n    slice StateChange Consumer\n      command C\n        reads Items\n    slice StateView Producer\n      readmodel Items\n"));
        var diagnostic = workspace.Compilation.Diagnostics.Single(value => value.Code == DiagnosticCodes.EventFromLaterSlice);
        var repair = WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).Single();
        var proposal = Propose(workspace, repair);
        proposal.Conflicts.ShouldBeEmpty();
        proposal.Workspace!.IdentityCatalog.Documents.ShouldEqual(workspace.Documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)));
    }
}
