// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_metadata_inspection_fails_after_a_completed_apply : given.a_competing_workspace_state
{
    JsonElement _arguments;
    JsonElement _result;
    JsonElement _read;
    JsonElement _previousConflict;
    string _proposalId = string.Empty;
    string _metadata = string.Empty;
    string _savedMetadata = string.Empty;
    Exception? _blockedRead;
    Exception? _discardedProposal;

    void Establish()
    {
        var opened = Result(Workspaces.Open(McpJson.Empty));
        _previousConflict = opened.GetProperty("rootBindingConflict");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        var proposal = Result(Workspaces.Propose(JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = Applied.Documents.Single().Id.ToString(), path = "Models/moved.play" }), false));
        _proposalId = proposal.GetProperty("proposalId").GetString()!;
        _arguments = JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, proposalId = _proposalId });
        _metadata = Path.Combine(ModelRoot, ".screenplay");
        _savedMetadata = Path.Combine(RootPath, "saved-metadata");
        Workspaces.AfterApply = () =>
        {
            Directory.Move(_metadata, _savedMetadata);
            File.WriteAllText(_metadata, "metadata directory replaced after verified apply");
        };
    }

    void Because()
    {
        _result = Result(Workspaces.Apply(_arguments));
        var expectedRevision = _result.GetProperty("workspace").GetProperty("revision").GetString();
        var readArguments = JsonSerializer.SerializeToElement(new { expectedRevision });
        _blockedRead = Catch.Exception(() => Workspaces.ReadWorkspace(readArguments));
        File.Delete(_metadata);
        Directory.Move(_savedMetadata, _metadata);
        _read = Result(Workspaces.ReadWorkspace(readArguments));
        _discardedProposal = Catch.Exception(() => Workspaces.DiscardProposal(JsonSerializer.SerializeToElement(new { proposalId = _proposalId })));
    }

    [Fact] void should_report_the_verified_apply_as_successful() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_report_an_unknown_apply_outcome() => _result.GetProperty("failureKind").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_the_metadata_problem() => _result.GetProperty("metadataProblem").GetString()!.Contains("MetadataPathConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_problematic_metadata_path() => _result.GetProperty("metadataProblem").GetString()!.Contains(_metadata, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_previous_conflict_snapshot() => _result.GetProperty("rootBindingConflict").GetRawText().ShouldEqual(_previousConflict.GetRawText());
    [Fact] void should_leave_the_completed_move_on_disk() => File.Exists(Path.Combine(ModelRoot, "moved.play")).ShouldBeTrue();
    [Fact] void should_not_restore_the_old_path() => File.Exists(Path.Combine(ModelRoot, "application.play")).ShouldBeFalse();
    [Fact] void should_leave_no_pending_outer_apply() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_refuse_further_reads_until_metadata_is_repaired() => _blockedRead!.Message.Contains("MetadataPathConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_applied_session_revision_without_reopening() => _read.GetProperty("workspace").GetProperty("revision").GetString().ShouldEqual(_result.GetProperty("workspace").GetProperty("revision").GetString());
    [Fact] void should_keep_session_identity_state_in_sync_with_disk() => _read.GetProperty("workspace").GetProperty("catalogRevision").GetString().ShouldEqual(McpState.Deserialize(Files.Read(McpState.FileName)!).Catalog.Revision.ToString());
    [Fact] void should_discard_the_completed_proposal() => ((McpFailure)_discardedProposal!).FailureKind.ShouldEqual("UnknownProposal");
}
