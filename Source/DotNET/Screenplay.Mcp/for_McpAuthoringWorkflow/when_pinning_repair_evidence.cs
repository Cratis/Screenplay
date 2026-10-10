// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_pinning_repair_evidence : given.an_authoring_connection
{
    const string AttachedSource = "policy IsAuthorized\n  file Handler.cs\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        projectId Uuid identifier\n        name String\n        produces Registered\n          name = name\n      event Registered\n        name String\n      command Anchor\n        anchorId Uuid identifier\n        produces Anchored\n          for anchorId\n      event Anchored\n";

    [Fact]
    void should_advertise_a_small_read_only_versioned_contract_without_opening_a_root()
    {
        Initialize();
        var capabilities = Result("repair-capabilities");
        capabilities.GetProperty("schemaVersion").GetInt32().ShouldEqual(1);
        capabilities.GetProperty("repairContractVersion").GetInt32().ShouldEqual(1);
        capabilities.GetProperty("actions").GetArrayLength().ShouldEqual(6);
        var duplicate = capabilities.GetProperty("actions").EnumerateArray().Single(action => action.GetProperty("diagnosticCode").GetString() == "PLAY0653");
        duplicate.GetProperty("requiredFormatting").GetString().ShouldEqual("PreserveTrivia");
        duplicate.GetProperty("pinRepairEvidence").GetBoolean().ShouldBeFalse();
        capabilities.GetProperty("actions").EnumerateArray().Where(action => string.Equals(action.GetProperty("diagnosticCode").GetString(), "PLAY0563", StringComparison.Ordinal) || string.Equals(action.GetProperty("diagnosticCode").GetString(), "PLAY0564", StringComparison.Ordinal)).All(action => !action.GetProperty("pinRepairEvidence").GetBoolean()).ShouldBeTrue();
        capabilities.GetProperty("cancellation").GetProperty("supported").GetBoolean().ShouldBeFalse();
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("PLAY0478")]
    [InlineData("PLAY0166")]
    void should_pin_one_selected_transaction_and_freeze_all_preview_views(string code)
    {
        var (opened, discovery) = Discover(code);
        var proposal = Propose(opened, discovery);
        var evidence = proposal.GetProperty("repairEvidence").GetProperty("beforeRevision").GetString();
        evidence.ShouldEqual(discovery.GetProperty("repairEvidenceRevision").GetString());
        var id = proposal.GetProperty("proposalId").GetString();
        foreach (var view in new[] { "changes", "diagnostics", "executable-diagnostics", "implementation-requirements", "dropped-comments" })
        {
            Result("read-proposal", new { proposalId = id, view }).GetProperty("repairEvidence").GetRawText().ShouldEqual(proposal.GetProperty("repairEvidence").GetRawText());
        }

        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "changed");
        foreach (var view in new[] { "changes", "before", "after", "diagnostics", "implementation-requirements" })
        {
            Failure(Call("read-proposal", new { proposalId = id, view, documentId = "ignored", offset = 1, limit = 1 })).ShouldEqual("RepairEvidenceDrift");
        }

        Failure(Call("apply", new
        {
            proposalId = id,
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        })).ShouldEqual("RepairEvidenceDrift");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(SourceFor(code));
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_install_exact_reviewed_bytes_with_pinned_evidence()
    {
        var (opened, discovery) = Discover("PLAY0478");
        var proposal = Propose(opened, discovery);
        var id = proposal.GetProperty("proposalId").GetString();
        var change = Result("read-proposal", new { proposalId = id }).GetProperty("result").GetProperty("items")[0];
        var page = Result("read-proposal", new { proposalId = id, view = "after", documentId = change.GetProperty("documentId").GetString() });
        var bytes = page.GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(bytes);
        var actual = Result("read-workspace", new { expectedRevision = proposal.GetProperty("after").GetProperty("revision").GetString(), view = "diagnostics" });
        actual.GetProperty("repairEvidenceRevision").GetString().ShouldEqual(proposal.GetProperty("repairEvidence").GetProperty("candidateRevision").GetString());
    }

    [Theory]
    [InlineData("application.play")]
    [InlineData("./folder/../application.play")]
    [InlineData(".screenplay/identities.json")]
    [InlineData(".screenplay/pending.json")]
    [InlineData(".screenplay")]
    [InlineData(".SCREENPLAY/IDENTITIES.JSON")]
    void should_refuse_model_selected_write_targets_before_advertising_an_accepted_preview(string attachment)
    {
        var (opened, discovery) = Discover("PLAY0478", attachment: attachment);
        var source = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        var current = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        current.GetProperty("workspace").GetProperty("catalogRevision").GetString().ShouldEqual(opened.GetProperty("catalogRevision").GetString());
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_an_imported_source_target_selected_by_the_root_model()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import \"Registration.play\"\npolicy IsAuthorized\n  file Registration.play\n");
        File.WriteAllText(Path.Combine(RootPath, "Registration.play"), AttachedSource[AttachedSource.IndexOf("module Projects", StringComparison.Ordinal)..]);
        Initialize();
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        var source = File.ReadAllBytes(Path.Combine(RootPath, "Registration.play"));
        Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllBytes(Path.Combine(RootPath, "Registration.play")).ShouldEqual(source);
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_not_ban_a_play_attachment_outside_the_recovery_owned_source_set()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "bin"));
        File.WriteAllText(Path.Combine(RootPath, "bin/Unchanged.play"), "concept Unchanged : String\n");
        var (opened, discovery) = Discover("PLAY0478", attachment: "bin/Unchanged.play");
        var proposal = Propose(opened, discovery);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        var actual = Result("read-workspace", new { expectedRevision = proposal.GetProperty("after").GetProperty("revision").GetString(), view = "diagnostics" });
        actual.GetProperty("repairEvidenceRevision").GetString().ShouldEqual(proposal.GetProperty("repairEvidence").GetProperty("candidateRevision").GetString());
    }

    [Fact]
    void should_refuse_an_unchanged_source_whose_access_rules_rollback_restores()
    {
        File.WriteAllText(Path.Combine(RootPath, "Unchanged.play"), "concept Unchanged : String\n");
        var (opened, discovery) = Discover("PLAY0478", attachment: "Unchanged.play");
        Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceWriteConflict");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_unpinned_source_attachment_behavior()
    {
        var (opened, discovery) = Discover("PLAY0478", attachment: "application.play");
        var repair = discovery.GetProperty("page").GetProperty("items")[0];
        var proposal = Result("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0478",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_candidate_only_attachment_that_selects_a_write_target()
    {
        _ = Discover("PLAY0478");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var syntax = new ScreenplayCompiler().Parse(AttachedSource.Replace("Handler.cs", "application.play", StringComparison.Ordinal)).Value!;
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
        workspace.AttachmentContents.ContainsKey("application.play").ShouldBeFalse();
        result.Workspace!.AttachmentContents.ContainsKey("application.play").ShouldBeTrue();
        var proposal = new McpAuthoringProposal(workspace, result, WorkspaceAuthoringValidation.Authoring, WorkspaceAuthoringReferencePolicy.Safe);
        (Catch.Exception(() => McpRepairEvidence.Pin(Root, proposal)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_an_existing_hard_link_to_a_source_write_target()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return; // Native Unix link scenario.
        _ = Discover("PLAY0478", attachment: "Alias.cs");
        var start = new System.Diagnostics.ProcessStartInfo("/bin/ln") { ArgumentList = { Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "Alias.cs") } };
        using var process = System.Diagnostics.Process.Start(start)!;
        process.WaitForExit(5000).ShouldBeTrue();
        process.ExitCode.ShouldEqual(0);
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceWriteConflict");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_existing_case_aliases_only_when_the_filesystem_resolves_them()
    {
        var (opened, discovery) = Discover("PLAY0478", attachment: "APPLICATION.PLAY");
        if (File.Exists(Path.Combine(RootPath, "APPLICATION.PLAY")))
        {
            Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceWriteConflict");
        }
        else
        {
            // Distinct spelling on a case-sensitive filesystem is a stable missing attachment, not overlap.
            var proposal = Propose(opened, discovery);
            Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        }
    }

    [Fact]
    void should_recheck_aliases_immediately_before_installation()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        _ = Discover("PLAY0478");

        // Equal bytes keep evidence unchanged while a new hard link changes physical overlap.
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), AttachedSource);
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        var workspaces = new McpWorkspaces(Root);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        var proposal = Structured(workspaces.ProposeRepair(JsonSerializer.SerializeToElement(Request(opened, discovery))));
        workspaces.BeforeInstall = () =>
        {
            File.Delete(Path.Combine(RootPath, "Handler.cs"));
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/ln") { ArgumentList = { Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "Handler.cs") } })!;
            process.WaitForExit(5000).ShouldBeTrue();
            process.ExitCode.ShouldEqual(0);
        };
        var applied = Structured(workspaces.Apply(JsonSerializer.SerializeToElement(new
        {
            proposalId = proposal.GetProperty("proposalId").GetString(),
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        })));
        applied.GetProperty("success").GetBoolean().ShouldBeFalse();
        applied.GetProperty("failureKind").GetString().ShouldEqual("RepairEvidenceWriteConflict");
        applied.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(AttachedSource);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_discovery_to_proposal_attachment_drift_without_source_or_catalog_change()
    {
        var (opened, discovery) = Discover("PLAY0478");
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "changed");
        var refreshed = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        refreshed.GetProperty("workspace").GetProperty("catalogRevision").GetString().ShouldEqual(opened.GetProperty("catalogRevision").GetString());
        Failure(Call("propose-repair", Request(opened, discovery))).ShouldEqual("RepairEvidenceDrift");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_retained_pages_when_a_loaded_file_disappears_or_becomes_unreadable(bool invalidUtf8)
    {
        var (_, discovery) = Discover("PLAY0478");
        var opened = Open();
        var proposal = Propose(opened, discovery);
        var path = Path.Combine(RootPath, "Handler.cs");
        if (invalidUtf8) File.WriteAllBytes(path, [0xff]);
        else File.Delete(path);
        Failure(Call("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "executable-diagnostics", offset = 1 })).ShouldEqual("RepairEvidenceDrift");
    }

    [Theory]
    [InlineData("PLAY0478", "missing", DiagnosticCodes.AttachmentMissing)]
    [InlineData("PLAY0478", "invalid-utf8", DiagnosticCodes.AttachmentUnreadable)]
    [InlineData("PLAY0478", "directory", DiagnosticCodes.AttachmentUnreadable)]
    [InlineData("PLAY0478", "refused", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData("PLAY0478", "oversized", DiagnosticCodes.AttachmentTooLarge)]
    [InlineData("PLAY0166", "missing", DiagnosticCodes.AttachmentMissing)]
    [InlineData("PLAY0166", "invalid-utf8", DiagnosticCodes.AttachmentUnreadable)]
    [InlineData("PLAY0166", "directory", DiagnosticCodes.AttachmentUnreadable)]
    [InlineData("PLAY0166", "refused", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData("PLAY0166", "oversized", DiagnosticCodes.AttachmentTooLarge)]
    void should_retain_unchanged_loading_warnings_through_validation_review_and_installation(string code, string state, string warning)
    {
        var (opened, discovery) = Discover(code, state);
        var before = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-diagnostics" });
        before.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.GetProperty("code").GetString() == warning).ShouldBeTrue();
        var proposal = Propose(opened, discovery);
        var id = proposal.GetProperty("proposalId").GetString();
        var diagnostics = Result("read-proposal", new { proposalId = id, view = "executable-diagnostics" }).GetProperty("result").GetProperty("items");
        diagnostics.EnumerateArray().Any(item => item.GetProperty("code").GetString() == warning).ShouldBeTrue();
        var ready = proposal.GetProperty("after").GetProperty("executableReady").GetBoolean();
        ready.ShouldBeTrue();
        var reviewed = Result("read-proposal", new { proposalId = id, view = "implementation-requirements" });
        reviewed.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldEqual(ready);
        var applied = Apply(opened, proposal);
        applied.GetProperty("success").GetBoolean().ShouldBeTrue();
        applied.GetProperty("workspace").GetProperty("executableReady").GetBoolean().ShouldEqual(ready);
    }

    [Theory]
    [InlineData("missing", "exists")]
    [InlineData("exists", "missing")]
    [InlineData("missing", "invalid-utf8")]
    [InlineData("invalid-utf8", "oversized")]
    void should_refuse_warning_state_changes_after_proof_at_retention(string before, string after)
    {
        var (opened, discovery) = Discover("PLAY0478", before);
        var workspaces = new McpWorkspaces(Root);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        workspaces.BeforeStore = () => SetAttachmentState(after);
        var failure = Catch.Exception(() => workspaces.ProposeRepair(JsonSerializer.SerializeToElement(Request(opened, discovery))));
        (failure as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceDrift");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_a_warning_state_change_after_retention_and_before_installation()
    {
        var (opened, discovery) = Discover("PLAY0478", "missing");
        var workspaces = new McpWorkspaces(Root);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        var proposal = Structured(workspaces.ProposeRepair(JsonSerializer.SerializeToElement(Request(opened, discovery))));
        workspaces.BeforeInstall = () => SetAttachmentState("exists");
        var applied = Structured(workspaces.Apply(JsonSerializer.SerializeToElement(new
        {
            proposalId = proposal.GetProperty("proposalId").GetString(),
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        })));
        applied.GetProperty("success").GetBoolean().ShouldBeFalse();
        applied.GetProperty("failureKind").GetString().ShouldEqual("RepairEvidenceDrift");
        applied.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(AttachedSource);
    }

    [Theory]
    [InlineData("missing", "exists")]
    [InlineData("exists", "missing")]
    [InlineData("invalid-utf8", "oversized")]
    void should_refuse_warning_state_changes_on_retained_preview_pages(string before, string after)
    {
        var (opened, discovery) = Discover("PLAY0478", before);
        var proposal = Propose(opened, discovery);
        SetAttachmentState(after);
        Failure(Call("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "executable-diagnostics", offset = 1 })).ShouldEqual("RepairEvidenceDrift");
    }

    [Fact]
    void should_keep_legacy_unpinned_repair_refresh_behavior()
    {
        var (opened, discovery) = Discover("PLAY0478");
        var repair = discovery.GetProperty("page").GetProperty("items")[0];
        var proposal = Result("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0478", subject = repair.GetProperty("subject"), formatting = "CanonicalizeTouchedDocuments"
        });
        var id = proposal.GetProperty("proposalId").GetString();
        string Hash() => Result("read-proposal", new { proposalId = id, view = "implementation-requirements" }).GetProperty("result").GetProperty("items")[0].GetProperty("contentHash").GetString()!;
        var before = Hash();
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "changed");
        Hash().ShouldNotEqual(before);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData("appear")]
    [InlineData("disappear")]
    [InlineData("invalid-utf8")]
    [InlineData("directory")]
    void should_pin_missing_resolved_and_loading_diagnostic_states(string transition)
    {
        var (opened, _) = Discover("PLAY0478");
        var path = Path.Combine(RootPath, "Handler.cs");
        if (transition == "appear") File.Delete(path);
        var before = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "diagnostics" });
        var evidence = before.GetProperty("repairEvidenceRevision").GetString();
        switch (transition)
        {
            case "appear": File.WriteAllText(path, "appeared"); break;
            case "disappear": File.Delete(path); break;
            case "invalid-utf8": File.WriteAllBytes(path, [0xff]); break;
            case "directory": File.Delete(path); Directory.CreateDirectory(path); break;
        }

        Failure(Call("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "diagnostics", expectedRepairEvidenceRevision = evidence })).ShouldEqual("RepairEvidenceDrift");
    }

    [Fact]
    void should_ignore_unloaded_files_and_implementation_locks()
    {
        var (opened, discovery) = Discover("PLAY0478");
        File.WriteAllText(Path.Combine(RootPath, "unreferenced.cs"), "irrelevant");
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        File.WriteAllText(Path.Combine(RootPath, ".screenplay", "implementations.json"), "not evidence");
        Propose(opened, discovery).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_refuse_validation_to_retention_drift_without_refreshing_proof()
    {
        var (opened, discovery) = Discover("PLAY0478");
        var workspaces = new McpWorkspaces(Root);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        workspaces.BeforeStore = () => File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "raced");
        var failure = Catch.Exception(() => workspaces.ProposeRepair(JsonSerializer.SerializeToElement(Request(opened, discovery))));
        (failure as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceDrift");
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Fact]
    void should_check_evidence_after_staging_immediately_before_installation()
    {
        var (opened, discovery) = Discover("PLAY0478");
        var workspaces = new McpWorkspaces(Root);
        _ = workspaces.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        var proposal = Structured(workspaces.ProposeRepair(JsonSerializer.SerializeToElement(Request(opened, discovery))));
        workspaces.BeforeInstall = () => File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "raced");
        var applied = Structured(workspaces.Apply(JsonSerializer.SerializeToElement(new
        {
            proposalId = proposal.GetProperty("proposalId").GetString(),
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        })));
        applied.GetProperty("success").GetBoolean().ShouldBeFalse();
        applied.GetProperty("failureKind").GetString().ShouldEqual("RepairEvidenceDrift");
        applied.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(AttachedSource);
    }

    [Fact]
    void should_refuse_different_candidate_resolution_instead_of_retaining_new_proof()
    {
        _ = Discover("PLAY0166");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var source = SourceFor("PLAY0166").Replace("Handler.cs", "Other.cs", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "Other.cs"), "other input");
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents[0].Id, syntax)]
        });
        result.Accepted.ShouldBeTrue();
        var proposal = new McpAuthoringProposal(workspace, result, WorkspaceAuthoringValidation.Authoring, WorkspaceAuthoringReferencePolicy.Safe);
        var refusal = Catch.Exception(() => McpRepairEvidence.Pin(Root, proposal));
        (refusal as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceDrift");
    }

    [Fact]
    void should_load_final_candidate_sources_once_before_validation_and_pin_their_own_warnings()
    {
        _ = Discover("PLAY0166");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var syntax = new ScreenplayCompiler().Parse(SourceFor("PLAY0166").Replace("Handler.cs", "Other.cs", StringComparison.Ordinal)).Value!;
        var calls = 0;
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Documents = [new ReplaceWorkspaceSyntaxDocument(workspace.Documents[0].Id, syntax)],
            AttachmentLoader = documents =>
            {
                calls++;
                documents.Single().Text.ShouldContain("Other.cs");
                return McpAttachmentContents.Load(Root, documents);
            }
        });
        calls.ShouldEqual(1);
        result.Accepted.ShouldBeTrue();
        result.Workspace!.AttachmentContents.ShouldBeEmpty();
        result.Workspace.AttachmentDiagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentMissing);
        result.ExecutableDiagnostics.ShouldContain(result.Workspace.AttachmentDiagnostics.Single());
        var proposal = new McpAuthoringProposal(workspace, result, WorkspaceAuthoringValidation.Authoring, WorkspaceAuthoringReferencePolicy.Safe);
        var evidence = McpRepairEvidence.Pin(Root, proposal);
        evidence.BeforeRevision.ShouldEqual(McpRepairEvidence.Revision(workspace));
        evidence.CandidateRevision.ShouldEqual(McpRepairEvidence.Revision(result.Workspace));
        File.WriteAllText(Path.Combine(RootPath, "Other.cs"), "now resolved");
        (Catch.Exception(() => evidence.Verify(Root, proposal)) as McpFailure)!.FailureKind.ShouldEqual("RepairEvidenceDrift");
    }

    [Fact]
    void should_run_only_one_fresh_selected_repair_transaction_with_candidate_loading()
    {
        _ = Discover("PLAY0478", "missing");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var repair = WorkspaceDiagnosticRepairs.Find(
            workspace,
            workspace.Revision,
            WorkspaceSyntaxIndex.Create(workspace).RepairableDiagnostics.First(diagnostic => diagnostic.Code == "PLAY0478")).Single();
        var before = WorkspaceRepairVerification.TransactionCount(workspace);
        var loads = 0;
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, "PLAY0478", repair.Subject, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            AttachmentLoader = documents =>
            {
                loads++;
                return McpAttachmentContents.Load(Root, documents);
            }
        });
        result.Accepted.ShouldBeTrue();
        loads.ShouldEqual(1);
        WorkspaceRepairVerification.TransactionCount(workspace).ShouldEqual(before + 1);
    }

    [Fact]
    void should_preserve_candidate_loader_diagnostics_without_dropping_semantic_errors()
    {
        _ = Discover("PLAY0166", "missing");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var error = Diagnostic.Error("HOST0001", "Host input failure", SourceLocation.Start);
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            AttachmentLoader = documents =>
            {
                var loaded = McpAttachmentContents.Load(Root, documents);
                return loaded with { Diagnostics = [.. loaded.Diagnostics, error] };
            }
        });
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeFalse();
        result.Workspace!.AttachmentDiagnostics.SequenceEqual([.. workspace.AttachmentDiagnostics, error]).ShouldBeTrue();
        result.Workspace.Compilation.Diagnostics.ShouldContain(error);
        result.ExecutableDiagnostics.ShouldContain(error);
        result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeTrue();
    }

    [Fact]
    void should_not_hide_a_candidate_loader_io_failure()
    {
        _ = Discover("PLAY0478");
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var failure = new IOException("Candidate loader failed");
        Catch.Exception(() => workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            AttachmentLoader = _ => throw failure
        })).ShouldEqual(failure);
    }

    [Fact]
    void should_invalidate_retained_pins_in_another_client_instance()
    {
        var (opened, discovery) = Discover("PLAY0478");
        var proposal = Propose(opened, discovery);
        var second = new McpWorkspaces(Root);
        _ = second.Open(JsonSerializer.SerializeToElement(new { applicationName = "Projects" }));
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "second client's input");
        var latest = Structured(second.ReadWorkspace(JsonSerializer.SerializeToElement(new { expectedRevision = opened.GetProperty("revision").GetString(), view = "diagnostics" })));
        latest.GetProperty("repairEvidenceRevision").GetString().ShouldNotEqual(discovery.GetProperty("repairEvidenceRevision").GetString());
        Failure(Call("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() })).ShouldEqual("RepairEvidenceDrift");
    }

    [Fact]
    void should_disclose_routing_change_and_disable_fix_all()
    {
        var (_, discovery) = Discover("PLAY0478");
        var repair = discovery.GetProperty("page").GetProperty("items")[0];
        repair.GetProperty("title").GetString().ShouldContain("Change routing");
        repair.GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    void should_refuse_malformed_evidence_and_capability_arguments_with_typed_rpc_data()
    {
        var (opened, discovery) = Discover("PLAY0478");
        Failure(Call("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), expectedRepairEvidenceRevision = "bad" })).ShouldEqual("InvalidArguments");
        Failure(Call("repair-capabilities", new { schemaVersion = 2 })).ShouldEqual("InvalidArguments");
        var repair = discovery.GetProperty("page").GetProperty("items")[0];
        Failure(Call("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0478", subject = repair.GetProperty("subject"), formatting = "CanonicalizeTouchedDocuments", pinRepairEvidence = true
        })).ShouldEqual("InvalidArguments");
    }

    static string SourceFor(string code) => code == "PLAY0166" ? AttachedSource.Replace("      event Registered\n        name String\n", "", StringComparison.Ordinal) : AttachedSource;

    (JsonElement Opened, JsonElement Discovery) Discover(string code, string attachmentState = "exists", string attachment = "Handler.cs")
    {
        var source = SourceFor(code).Replace("Handler.cs", attachment, StringComparison.Ordinal);
        if (attachmentState == "refused") source = source.Replace("Handler.cs", "../Handler.cs", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        SetAttachmentState(attachmentState);
        Initialize();
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        if (discovery.GetProperty("page").GetProperty("items").GetArrayLength() == 0)
        {
            throw new McpFailure(Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-diagnostics" }).GetRawText());
        }

        return (opened, discovery);
    }

    void SetAttachmentState(string state)
    {
        var path = Path.Combine(RootPath, "Handler.cs");
        switch (state)
        {
            case "missing": File.Delete(path); break;
            case "invalid-utf8": File.WriteAllBytes(path, [0xff]); break;
            case "directory": File.Delete(path); Directory.CreateDirectory(path); break;
            case "oversized": File.WriteAllBytes(path, new byte[AttachmentFiles.MaximumFileBytes + 1]); break;
            default: File.WriteAllText(path, "first"); break;
        }
    }

    static object Request(JsonElement opened, JsonElement discovery)
    {
        var repair = discovery.GetProperty("page").GetProperty("items").EnumerateArray().First();
        return new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = repair.GetProperty("diagnosticCode").GetString(),
            subject = repair.GetProperty("subject"), formatting = "CanonicalizeTouchedDocuments", pinRepairEvidence = true,
            expectedRepairEvidenceRevision = discovery.GetProperty("repairEvidenceRevision").GetString()
        };
    }

    JsonElement Propose(JsonElement opened, JsonElement discovery) => Result("propose-repair", Request(opened, discovery));

    static JsonElement Structured(object response) => JsonSerializer.SerializeToElement(response, McpJson.Options).GetProperty("structuredContent");

    static string Failure(JsonElement response) => response.TryGetProperty("error", out var error)
        ? error.GetProperty("data").GetProperty("failureKind").GetString()!
        : response.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind").GetString()!;
}
