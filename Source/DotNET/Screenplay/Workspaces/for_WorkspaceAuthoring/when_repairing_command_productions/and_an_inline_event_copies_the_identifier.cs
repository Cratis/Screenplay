// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_an_inline_event_copies_the_identifier : given.a_command_production
{
    const string InlineSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          projectId Uuid = projectId\n          name String = \"something\"\n";

    void Establish()
    {
        Create(InlineSource);
        Repair = Find(DiagnosticCodes.EventSourceIdInPayload);
    }

    void Because() => Result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request());

    [Fact] void should_accept_the_explicit_contract_change() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_label_the_consequence() => Repair.Title!.ShouldContain("changes the event contract");
    [Fact] void should_exclude_it_from_fix_all() => Repair.CanFixAll.ShouldBeFalse();
    [Fact] void should_retire_the_payload_property() => Repair.RetiredSemanticAddresses.Length.ShouldEqual(1);
    [Fact] void should_remove_the_diagnostic() => Result.Workspace!.Compilation.Diagnostics.Any(value => value.Code == DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
    [Fact] void should_remove_both_typed_nodes() => Repair.Operations.OfType<RemoveWorkspaceNode>().Count().ShouldEqual(2);
    [Fact] void should_keep_the_command_identifier() => WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(value => value.Node).OfType<CommandSyntax>().Single().Properties.Single().IsIdentifier.ShouldBeTrue();
    [Fact] void should_keep_the_contract_identity() => Result.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(Workspace.IdentityCatalog.EventContracts.Single().Id);

    [Theory]
    [InlineData("behavior Observe\n  on event Renamed\n    notify info \"Changed\"\n")]
    [InlineData("      screen Observe\n        on event Renamed\n          notify info \"Changed\"\n")]
    [InlineData("      readmodel Names\n        name String\n      projection Names => Names\n        all\n          name = \"something\"\n")]
    [InlineData("      readmodel Names\n        projectId Uuid\n      projection Names => Names\n        from Renamed\n          projectId = projectId\n")]
    [InlineData("      specification Example\n        given Renamed\n          for \"00000000-0000-0000-0000-000000000001\"\n          projectId = \"00000000-0000-0000-0000-000000000001\"\n          name = \"something\"\n        when Rename\n          projectId = \"00000000-0000-0000-0000-000000000001\"\n        then Renamed\n          for \"00000000-0000-0000-0000-000000000001\"\n          projectId = \"00000000-0000-0000-0000-000000000001\"\n          name = \"something\"\n", WorkspaceConflictKind.CompilationFailed)]
    [InlineData("      command Other\n        projectId Uuid identifier\n        produces Renamed\n          for projectId\n          projectId = projectId\n          name = \"something\"\n", WorkspaceConflictKind.CompilationFailed)]
    void should_not_offer_a_repair_with_consumers(string consumer, WorkspaceConflictKind expectedConflict = WorkspaceConflictKind.InvalidOperation)
    {
        Create(InlineSource + consumer);
        WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.Any(value => value.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
        WorkspaceSyntaxIndex.Create(Workspace).RepairableDiagnostics.Any(value => value.Code == DiagnosticCodes.EventSourceIdInPayload).ShouldBeTrue();
        HasRepair(DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
        WorkspaceRepairVerification.TransactionCount(Workspace).ShouldEqual(1);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var mapping = index.Entries.Single(entry => entry.Node is PropertyMappingSyntax property && property.Property == "projectId" &&
            index.Find(entry.Parent!)?.Node is ProducesSyntax { InlineEvent: not null });
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.EventSourceIdInPayload, mapping.Handle, Request());
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Kind.ShouldEqual(expectedConflict);
        WorkspaceRepairVerification.TransactionCount(Workspace).ShouldEqual(2);
    }

    [Fact]
    void should_not_remove_commented_payload_lines()
    {
        Create(InlineSource.Replace("projectId Uuid = projectId", "projectId Uuid = projectId // why this matters", StringComparison.Ordinal));
        HasRepair(DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
        var mapping = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(value => value.Node is PropertyMappingSyntax node && node.Property == "projectId");
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, DiagnosticCodes.EventSourceIdInPayload, mapping.Handle, Request());
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.RepairWouldDropComments);
    }

    [Fact]
    void should_refuse_changes_to_a_sibling_destination()
    {
        Create(InlineSource.Replace("        produces event Renamed", "        produces event Renamed\n          for projectId", StringComparison.Ordinal) + "        produces Other\n      event Other\n");
        HasRepair(DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
    }

    [Fact]
    void should_not_offer_a_plain_contract_repair()
    {
        Create(InlineSource.Replace("produces event Renamed", "produces Renamed\n          for projectId", StringComparison.Ordinal)
            .Replace("projectId Uuid =", "projectId =", StringComparison.Ordinal).Replace("name String =", "name =", StringComparison.Ordinal) + "      event Renamed\n        projectId Uuid\n        name String\n");
        HasRepair(DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
    }
}
