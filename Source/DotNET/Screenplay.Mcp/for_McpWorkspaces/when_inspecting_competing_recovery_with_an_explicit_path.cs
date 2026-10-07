// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_inspecting_competing_recovery_with_an_explicit_path : given.a_competing_workspace_state
{
    McpRecoveryJournal _journal = null!;
    JsonElement _opened;
    JsonElement _status;

    void Establish()
    {
        _journal = PrepareNestedJournal();
        Call("open-workspace").GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    void Because()
    {
        _opened = Call("open-workspace", new { path = ModelRoot }).GetProperty("result");
        _status = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_still_require_recovery_before_opening_the_model() => _opened.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_offer_the_competing_operation_at_its_explicit_root() => _status.GetProperty("recovery").GetProperty("operationId").GetString().ShouldEqual(_journal.Record.OperationId);
    [Fact] void should_offer_rollback_at_the_explicit_root() => _status.GetProperty("recovery").GetProperty("canRollback").GetBoolean().ShouldBeTrue();
    [Fact] void should_clear_the_discovery_conflict_on_the_failed_explicit_open() => _opened.GetProperty("structuredContent").TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
    [Fact] void should_clear_the_discovery_conflict_on_status() => _status.TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
    [Fact] void should_not_migrate_or_recover_the_journal_implicitly() => NestedFiles.Read(McpRecoveryJournal.FileName).ShouldNotBeNull();
}
