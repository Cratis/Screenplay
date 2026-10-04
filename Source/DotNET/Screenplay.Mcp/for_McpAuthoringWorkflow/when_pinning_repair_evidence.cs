// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
        capabilities.GetProperty("actions").GetArrayLength().ShouldEqual(2);
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

    (JsonElement Opened, JsonElement Discovery) Discover(string code)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), SourceFor(code));
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "first");
        Initialize();
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        if (discovery.GetProperty("page").GetProperty("items").GetArrayLength() == 0)
        {
            throw new McpFailure(Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-diagnostics" }).GetRawText());
        }

        return (opened, discovery);
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
