// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_a_competing_journal_appears_after_proposing : given.a_competing_workspace_state
{
    object _proposalArguments = null!;
    object _applyArguments = null!;
    byte[] _marker = [];
    readonly List<JsonElement> _errors = [];

    void Establish()
    {
        var opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        _proposalArguments = new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = Applied.Documents.Single().Id.ToString(), path = "Models/moved.play" };
        var proposal = Call("propose", _proposalArguments).GetProperty("result").GetProperty("structuredContent");
        _applyArguments = new { expectedRevision, expectedCatalogRevision, proposalId = proposal.GetProperty("proposalId").GetString() };
        PrepareNestedJournal();
        _marker = NestedFiles.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        _errors.Add(Call("open-workspace").GetProperty("result"));
        _errors.Add(Call("propose", _proposalArguments).GetProperty("result"));
        _errors.Add(Call("apply", _applyArguments).GetProperty("result"));
    }

    [Fact] void should_refuse_open_propose_and_apply() => _errors.TrueForAll(error => error.GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_report_pending_operations_for_all_three_calls() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("failureKind").GetString() == "PendingOperation").ShouldBeTrue();
    [Fact] void should_name_the_competing_journal_in_every_error() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_disclose_the_pending_root_in_every_error() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("pendingRoots")[0].GetString() == ModelRoot).ShouldBeTrue();
    [Fact] void should_leave_the_proposed_move_unapplied() => File.Exists(Path.Combine(ModelRoot, "moved.play")).ShouldBeFalse();
    [Fact] void should_leave_the_original_source_unchanged() => NestedRoot.Read().Single().Bytes.ToArray().ShouldEqual([.. NestedWorkspace.Documents.Single().Bytes]);
    [Fact] void should_preserve_outer_identities() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
    [Fact] void should_preserve_nested_identities() => NestedFiles.Read(McpState.FileName).ShouldEqual(NestedState);
    [Fact] void should_preserve_the_pending_journal() => NestedFiles.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
    [Fact] void should_not_start_a_competing_outer_apply() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
