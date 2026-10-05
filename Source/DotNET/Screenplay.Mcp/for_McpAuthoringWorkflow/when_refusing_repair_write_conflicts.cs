// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_refusing_repair_write_conflicts : given.an_authoring_connection
{
    const string OperationId = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.stage")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.backup")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.journal")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.state-rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-0.rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-1.rollback")]
    [InlineData("z-unchanged.play")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.stage")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.backup")]
    void should_include_all_referencable_operation_owned_artifacts(string reference)
    {
        var proposal = Proposal(reference, unchangedDocument: true);
        proposal.Before.Documents.Length.ShouldEqual(2);
        proposal.WritePlan.Entries.Length.ShouldEqual(1);
        proposal.WritePlan.Entries.Single().Before!.Path.Value.ShouldEqual("application.play");
        var source = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        var refusal = Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId));
        (refusal as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

#pragma warning disable CRSPEC0004 // The custom xUnit Fact attribute reports a genuine native-volume skip.
    [WindowsShortNamesFact]
    void should_refuse_a_native_short_alias_before_identity_state_is_created()
    {
        var metadata = Path.Combine(RootPath, ".screenplay");
        McpFileAccess.CreatePrivateDirectory(metadata);
        var state = Path.Combine(metadata, "identities.json");
        File.WriteAllText(state, "native alias probe");
        var alias = Path.GetFileName(WindowsShortNamesFactAttribute.ShortPath(state));
        alias.Contains('~').ShouldBeTrue();
        File.Delete(state);
        var proposal = Proposal($".screenplay/{alias}");
        (Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.Exists(state).ShouldBeFalse();
        File.Exists(Path.Combine(metadata, "pending.json")).ShouldBeFalse();
    }

    [WindowsShortNamesFact]
    void should_refuse_a_missing_short_alias_of_identity_state_that_will_be_replaced()
    {
        var metadata = Path.Combine(RootPath, ".screenplay");
        McpFileAccess.CreatePrivateDirectory(metadata);
        var state = Path.Combine(metadata, "identities.json");
        File.WriteAllText(state, "unchanged identity preimage");
        File.Exists(Path.Combine(metadata, "IDENTI~9.JSO")).ShouldBeFalse();
        var proposal = Proposal(".screenplay/IDENTI~9.JSO");
        (Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllText(state).ShouldEqual("unchanged identity preimage");
        File.Exists(Path.Combine(metadata, "pending.json")).ShouldBeFalse();
    }

    [WindowsShortNamesFact]
    void should_check_a_native_short_alias_in_a_missing_metadata_parent()
    {
        var metadata = Path.Combine(RootPath, ".screenplay");
        McpFileAccess.CreatePrivateDirectory(metadata);
        var alias = Path.GetFileName(WindowsShortNamesFactAttribute.ShortPath(metadata));
        alias.Contains('~').ShouldBeTrue();
        Directory.Delete(metadata);
        var proposal = Proposal($"{alias}/identities.json");
        (Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        Directory.Exists(metadata).ShouldBeFalse();
    }
#pragma warning restore CRSPEC0004

    [Fact]
    void should_not_treat_another_operations_missing_artifact_as_a_write_target()
    {
        var proposal = Proposal(".screenplay/ffffffffffffffffffffffffffffffff.stage");
        Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId)).ShouldBeNull();
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_unchanged_link_refusal_evidence_without_following_the_attachment()
    {
        if (OperatingSystem.IsWindows()) return; // Symlink privileges are not assumed on Windows.
        var metadata = Path.Combine(RootPath, ".screenplay");
        McpFileAccess.CreatePrivateDirectory(metadata);
        File.CreateSymbolicLink(Path.Combine(metadata, "Alias.cs"), Path.Combine(RootPath, "application.play"));
        var proposal = Proposal(".screenplay/Alias.cs");
        proposal.Workspace.AttachmentDiagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);
        var pin = McpRepairEvidence.Pin(Root, proposal);
        new McpDisk(Root).Apply(proposal, verifyEvidence: () => pin.Verify(Root, proposal), operationId: pin.OperationId).Success.ShouldBeTrue();
        McpRepairEvidence.Revision(McpAttachmentContents.Refresh(Root, proposal.Workspace)).ShouldEqual(pin.CandidateRevision);
        proposal.Workspace.AttachmentDiagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);
        File.Delete(Path.Combine(metadata, "Alias.cs"));
    }

    [Fact]
    void should_refuse_an_existing_hard_link_to_identity_state_without_reading_a_lock()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var metadata = Path.Combine(RootPath, ".screenplay");
        McpFileAccess.CreatePrivateDirectory(metadata);
        var state = Path.Combine(metadata, "identities.json");
        File.WriteAllText(state, "state input");
        var alias = Path.Combine(RootPath, "StateAlias.cs");
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/ln") { ArgumentList = { state, alias } })!;
        process.WaitForExit(5000).ShouldBeTrue();
        process.ExitCode.ShouldEqual(0);
        var proposal = Proposal("StateAlias.cs");
        var refusal = Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId));
        (refusal as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllText(state).ShouldEqual("state input");
        File.Exists(Path.Combine(metadata, "pending.json")).ShouldBeFalse();
        File.Exists(Path.Combine(metadata, "implementations.json")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_a_base_only_reference_to_a_source_write_target()
    {
        var proposal = Proposal("application.play", candidateReference: "Handler.cs");
        proposal.Before.AttachmentContents.ContainsKey("application.play").ShouldBeTrue();
        proposal.Workspace.AttachmentContents.ContainsKey("application.play").ShouldBeFalse();
        (Catch.Exception(() => McpRepairEvidence.Pin(Root, proposal)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
    }

    [Theory]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-0.rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-1.rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.state-rollback")]
    void should_refuse_a_hard_link_to_each_rollback_target(string target)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var proposal = Proposal("Alias.cs", unchangedDocument: true);
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        var path = Path.Combine(RootPath, target);
        File.WriteAllText(path, "attachment input");
        HardLink(path, Path.Combine(RootPath, "Alias.cs"));
        (Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllText(path).ShouldEqual("attachment input");
        File.ReadAllText(Path.Combine(RootPath, "Alias.cs")).ShouldEqual("attachment input");
        File.Exists(Path.Combine(RootPath, ".screenplay/pending.json")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef-0.rollback")]
    [InlineData("0123456789abcdef0123456789abcdef-1.rollback")]
    [InlineData("0123456789abcdef0123456789abcdef.state-rollback")]
    void should_use_physical_identity_for_existing_case_aliases_of_each_rollback_target(string name)
    {
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        var target = Path.Combine(RootPath, ".screenplay", name);
        File.WriteAllText(target, "attachment input");
        var alias = Path.Combine(RootPath, ".screenplay", name.ToUpperInvariant());
        var proposal = Proposal($".screenplay/{name.ToUpperInvariant()}", unchangedDocument: true);
        var failure = Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId));
        if (File.Exists(alias)) (failure as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        else failure.ShouldBeNull();
        File.ReadAllText(target).ShouldEqual("attachment input");
    }

    [Fact]
    void should_preserve_the_attachment_when_a_late_rollback_alias_causes_apply_to_roll_back()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var proposal = Proposal("Alias.cs");
        var attachment = Path.Combine(RootPath, "Alias.cs");
        var original = proposal.Before.Documents[0].Bytes.ToArray();
        File.WriteAllBytes(attachment, original);
        proposal = Proposal("Alias.cs");
        var pin = McpRepairEvidence.Pin(Root, proposal);
        var rollback = McpRecoveryJournal.RestorePath(Root, pin.OperationId, 0);
        var result = new McpDisk(Root).Apply(
            proposal,
            verifyEvidence: () =>
            {
                HardLink(attachment, rollback);
                pin.Verify(Root, proposal);
            },
            operationId: pin.OperationId);
        result.Success.ShouldBeFalse();
        result.Status.ShouldEqual("RolledBack");
        result.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllBytes(attachment).ShouldEqual(original);
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(original);
        File.Exists(Path.Combine(RootPath, ".screenplay/pending.json")).ShouldBeFalse();
        File.Exists(rollback).ShouldBeFalse();
    }

    [Fact]
    void should_match_the_shared_manifest_to_all_journal_and_recovery_targets()
    {
        var proposal = Proposal("Handler.cs", unchangedDocument: true);
        var manifest = McpRecoveryJournal.PlannedPaths(Root, proposal, OperationId).Distinct().Order(StringComparer.Ordinal).ToArray();
        var journal = McpRecoveryJournal.Prepare(Root, proposal, new(null, McpState.Serialize(proposal.Workspace)), OperationId);
        var targets = journal.Changes.SelectMany(change => new[] { change.Stage, change.Backup, change.Entry.Before is null ? null : Root.PathFor(change.Entry.Before.Path), change.Entry.After is null ? null : Root.PathFor(change.Entry.After.Path) })
            .OfType<string>().Concat(journal.Before.Documents.Select(document => Root.PathFor(document.Path)))
            .Concat(Enumerable.Range(0, journal.Before.Documents.Length).Select(journal.RestoreStage))
            .Concat([journal.StateRestoreStage, journal.StateStage, journal.StateBackup, McpRecoveryJournal.OperationPath(Root, OperationId, "journal"),
                new McpManagedFiles(Root).PathFor(McpState.FileName), new McpManagedFiles(Root).PathFor(McpRecoveryJournal.FileName), Path.Combine(RootPath, ".screenplay")])
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        manifest.ShouldEqual(targets);
        new McpRecovery(Root, journal).Rollback();
    }

    [Fact]
    void should_preserve_already_matching_source_access_rules_during_restore()
    {
        var path = Path.Combine(RootPath, "application.play");
        var access = McpRecoveryAccess.Capture("application.play", path);
        access.Restore(path);
        McpRecoveryAccess.Capture("application.play", path).Rules.ShouldEqual(access.Rules);
        Catch.Exception(() => access.Verify(path)).ShouldBeNull();
    }

    [Fact]
    void should_include_every_parent_that_source_installation_can_create()
    {
        var workspace = Workspace();
        var proposal = new McpProposal(workspace, workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new MoveWorkspaceDocument { Document = workspace.Documents[0].Id, Path = PortablePlayPath.Parse("long directory/nested/renamed.play") }]
        }));
        var manifest = McpRecoveryJournal.PlannedPaths(Root, proposal, OperationId).ToHashSet(StringComparer.Ordinal);
        manifest.Contains(Path.Combine(RootPath, "long directory")).ShouldBeTrue();
        manifest.Contains(Path.Combine(RootPath, "long directory", "nested")).ShouldBeTrue();
        manifest.Contains(Path.Combine(RootPath, "long directory", "nested", "renamed.play")).ShouldBeTrue();
        manifest.Contains(Path.Combine(RootPath, "long directory", "nested", $"renamed.play.screenplay-mcp-{OperationId}.stage")).ShouldBeTrue();
        Directory.Exists(Path.Combine(RootPath, "long directory")).ShouldBeFalse();
    }

    static void HardLink(string source, string destination)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/ln") { ArgumentList = { source, destination } })!;
        process.WaitForExit(5000).ShouldBeTrue();
        process.ExitCode.ShouldEqual(0);
    }

    McpAuthoringProposal Proposal(string reference, string? candidateReference = null, bool unchangedDocument = false)
    {
        if (unchangedDocument) File.WriteAllText(Path.Combine(RootPath, "z-unchanged.play"), "concept Unchanged : String\n");
        var before = $"policy IsAuthorized\n  file {reference}\n{Source}\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), before);
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var after = before.Replace("Registers a new project", "Registers a changed project", StringComparison.Ordinal);
        if (candidateReference is not null) after = after.Replace(reference, candidateReference, StringComparison.Ordinal);
        var syntax = new ScreenplayCompiler().Parse(after).Value!;
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents.Single(document => document.Path.Value == "application.play").Id, syntax)],
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        });
        result.Accepted.ShouldBeTrue();

        return new(workspace, result, WorkspaceAuthoringValidation.Authoring, WorkspaceAuthoringReferencePolicy.Safe);
    }
}
