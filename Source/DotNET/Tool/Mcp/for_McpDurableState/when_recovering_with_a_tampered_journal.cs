// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_recovering_with_a_tampered_journal : given.a_durable_workspace
{
    McpRecoveryJournal _journal = null!;
    JsonElement _result;
    JsonElement _status;

    void Establish()
    {
        _journal = Prepare();
        Interrupt(_journal, installState: true);
        File.WriteAllText(Files.PathFor(McpRecoveryJournal.FileName), "{\"Version\":999}");
        Initialize();
    }

    void Because()
    {
        _result = Call("recover-workspace", new { operationId = _journal.Record.OperationId }).GetProperty("result");
        _status = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_refuse_recovery() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_a_recovery_journal_conflict() => _result.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("RecoveryJournalConflict");
    [Fact] void should_report_that_rollback_is_not_available() => _status.GetProperty("recovery").GetProperty("canRollback").GetBoolean().ShouldBeFalse();
    [Fact] void should_preserve_tampered_bytes_for_manual_recovery() => File.ReadAllText(Files.PathFor(McpRecoveryJournal.FileName)).ShouldEqual("{\"Version\":999}");
    [Fact] void should_preserve_installed_source() => File.ReadAllText(Path.Combine(RootPath, "renamed.play")).ShouldEqual(Source);
    [Fact] void should_preserve_installed_identities() => Files.Read(McpState.FileName).ShouldEqual(_journal.Record.AfterState);
    [Fact] void should_preserve_the_original_source_backup() => File.ReadAllText(_journal.Changes.Single().Backup!).ShouldEqual(Source);
    [Fact] void should_preserve_the_original_identity_backup() => File.ReadAllBytes(_journal.StateBackup).ShouldEqual(OriginalState);
}
