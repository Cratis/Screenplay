// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_an_io_failure_follows_a_completed_apply : given.a_competing_workspace_state
{
    JsonElement _arguments;
    JsonElement _result;
    JsonElement _previousConflict;

    void Establish()
    {
        var opened = Result(Workspaces.Open(McpJson.Empty));
        _previousConflict = opened.GetProperty("rootBindingConflict");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        var proposal = Result(Workspaces.Propose(JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, operation = "move-document", documentId = Applied.Documents.Single().Id.ToString(), path = "Models/moved.play" }), false));
        _arguments = JsonSerializer.SerializeToElement(new { expectedRevision, expectedCatalogRevision, proposalId = proposal.GetProperty("proposalId").GetString() });
        Workspaces.AfterApply = () => throw new IOException("The competing metadata folder could not be read.");
    }

    void Because() => _result = Result(Workspaces.Apply(_arguments));

    [Fact] void should_report_the_verified_apply_as_successful() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_report_an_unknown_apply_outcome() => _result.GetProperty("failureKind").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_the_io_problem() => _result.GetProperty("metadataProblem").GetString().ShouldEqual("The competing metadata folder could not be read.");
    [Fact] void should_preserve_the_previous_conflict_snapshot() => _result.GetProperty("rootBindingConflict").GetRawText().ShouldEqual(_previousConflict.GetRawText());
    [Fact] void should_leave_the_completed_move_on_disk() => File.Exists(Path.Combine(ModelRoot, "moved.play")).ShouldBeTrue();
}
