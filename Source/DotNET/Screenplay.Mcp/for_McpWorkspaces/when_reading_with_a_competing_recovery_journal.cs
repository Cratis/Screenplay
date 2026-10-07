// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_reading_with_a_competing_recovery_journal : given.a_competing_workspace_state
{
    readonly List<JsonElement> _errors = [];
    Exception? _visualizationError;
    byte[] _marker = [];

    void Establish()
    {
        Call("describe-application").GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        PrepareNestedJournal();
        _marker = NestedFiles.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        _errors.Add(Call("describe-application").GetProperty("result"));
        _errors.Add(Call("merged-document").GetProperty("result"));
        _errors.Add(Call("read-document", new { path = "Models/application.play" }).GetProperty("result"));
        _visualizationError = Catch.Exception(() => Workspaces.Visualize(McpJson.Empty));
    }

    [Fact] void should_refuse_every_source_query_including_the_cached_query() => _errors.TrueForAll(error => error.GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_report_pending_operations_for_source_queries() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("failureKind").GetString() == "PendingOperation").ShouldBeTrue();
    [Fact] void should_name_the_competing_journal_root_in_every_error() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_competing_journal_in_structured_data() => _errors.TrueForAll(error => error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("pendingRoots")[0].GetString() == ModelRoot).ShouldBeTrue();
    [Fact] void should_refuse_visualization_with_a_pending_operation() => ((McpFailure)_visualizationError!).FailureKind.ShouldEqual("PendingOperation");
    [Fact] void should_name_the_journal_root_in_the_visualization_failure() => _visualizationError!.Message.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_competing_journal() => NestedFiles.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
}
