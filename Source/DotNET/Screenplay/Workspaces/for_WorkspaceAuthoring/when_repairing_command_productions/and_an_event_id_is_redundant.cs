// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_an_event_id_is_redundant : given.a_command_production
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_remove_only_the_pin_in_one_fresh_transaction(bool inline)
    {
        var source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n" +
            (inline ? "        produces event Renamed\n          id \"Renamed\"\n          name String = \"name\"\n" :
                "        produces Renamed\n          for projectId\n          name = \"name\"\n      event Renamed\n        id \"Renamed\"\n        name String\n");
        Create(source);
        var diagnostic = WorkspaceSyntaxIndex.Create(Workspace).RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.RedundantEventId);
        var repair = WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, diagnostic).Single();
        var transactions = WorkspaceProductionRepairs.TransactionCount(Workspace);
        WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, diagnostic).Length.ShouldEqual(1);
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(transactions);
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, diagnostic.Code, repair.Subject, Request());
        result.Accepted.ShouldBeTrue();
        WorkspaceProductionRepairs.TransactionCount(Workspace).ShouldEqual(transactions + 1);
        WorkspaceRepairVerification.SameModel(Workspace, result.Workspace!).ShouldBeTrue();
        result.Workspace!.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
        WorkspaceSyntaxIndex.Create(result.Workspace).Entries.Select(value => value.Node).OfType<EventSyntax>().Single().Id.ShouldBeNull();
    }

    [Fact]
    void should_not_advertise_a_comment_losing_removal()
    {
        Create(DestinationSource.Replace("      event ProjectRegistered", "      event ProjectRegistered\n        id \"ProjectRegistered\" // keep identity explanation", StringComparison.Ordinal));
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var diagnostic = index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.RedundantEventId);
        var subject = index.Entries.Single(value => value.Node is EventSyntax declaration && declaration.Name == "ProjectRegistered");
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, diagnostic.Code, subject.Handle, Request());
        if (result.Accepted)
        {
            result.Workspace!.Documents[0].Text.ShouldContain("// keep identity explanation");
            WorkspaceDiagnosticRepairs.Find(index, Workspace.Revision, diagnostic).Length.ShouldEqual(1);
        }
        else
        {
            result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.RepairWouldDropComments);
            WorkspaceDiagnosticRepairs.Find(index, Workspace.Revision, diagnostic).ShouldBeEmpty();
        }
    }
}
