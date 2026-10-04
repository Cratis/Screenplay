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
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.stage")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.backup")]
    void should_include_all_referencable_operation_owned_artifacts(string reference)
    {
        var proposal = Proposal(reference);
        var source = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        var refusal = Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId));
        (refusal as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

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

    McpAuthoringProposal Proposal(string reference, string? candidateReference = null)
    {
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
            Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents[0].Id, syntax)],
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        });
        result.Accepted.ShouldBeTrue();

        return new(workspace, result, WorkspaceAuthoringValidation.Authoring, WorkspaceAuthoringReferencePolicy.Safe);
    }
}
