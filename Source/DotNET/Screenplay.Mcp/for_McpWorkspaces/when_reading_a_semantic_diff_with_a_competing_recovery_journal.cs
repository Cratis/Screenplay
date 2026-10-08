// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_reading_a_semantic_diff_with_a_competing_recovery_journal : given.a_competing_workspace_state
{
    string _proposalId = string.Empty;
    JsonElement _error;

    void Establish()
    {
        var opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        var proposal = Call("propose", new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = Applied.Documents.Single().Id.ToString(), path = "Models/moved.play" })
            .GetProperty("result").GetProperty("structuredContent");
        _proposalId = proposal.GetProperty("proposalId").GetString()!;
        PrepareNestedJournal();
    }

    void Because() => _error = Call("read-proposal", new { proposalId = _proposalId, view = "semantic-diff" }).GetProperty("result");

    [Fact] void should_refuse_the_semantic_diff() => _error.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_a_pending_operation() => _error.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_name_the_competing_journal() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
}
