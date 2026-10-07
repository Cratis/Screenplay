// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_a_competing_journal_appears_before_installing : given.a_competing_workspace_state
{
    JsonElement _arguments;
    JsonElement _result;

    void Establish()
    {
        var opened = Result(Workspaces.Open(McpJson.Empty));
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        var proposal = Result(Workspaces.Propose(JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = Applied.Documents.Single().Id.ToString(), path = "Models/moved.play" }), false));
        _arguments = JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, proposalId = proposal.GetProperty("proposalId").GetString() });
        Workspaces.BeforeInstall = () => PrepareNestedJournal();
    }

    void Because() => _result = Result(Workspaces.Apply(_arguments));

    [Fact] void should_refuse_the_installation() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_pending_operation() => _result.GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_install_no_documents() => _result.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
    [Fact] void should_preserve_the_original_source() => NestedRoot.Read().Single().Bytes.ToArray().ShouldEqual([.. NestedWorkspace.Documents.Single().Bytes]);
    [Fact] void should_preserve_outer_identities() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
    [Fact] void should_preserve_the_competing_journal() => NestedFiles.Read(McpRecoveryJournal.FileName).ShouldNotBeNull();
    [Fact] void should_clear_only_the_rolled_back_outer_journal() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_report_only_the_remaining_pending_root() => _result.GetProperty("rootBindingConflict").GetProperty("pendingRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([ModelRoot]);
}
