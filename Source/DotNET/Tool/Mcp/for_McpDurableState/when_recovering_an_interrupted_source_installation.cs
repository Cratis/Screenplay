// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_recovering_an_interrupted_source_installation : given.a_durable_workspace
{
    string _operation = string.Empty;
    Exception _blocked = null!;
    JsonElement _result;

    void Establish()
    {
        var journal = Prepare();
        _operation = journal.Record.OperationId;
        Interrupt(journal, installState: false);
    }

    void Because()
    {
        var freshSession = new McpWorkspaces(new McpRoot(RootPath));
        _blocked = Catch.Exception(() => freshSession.Open(McpJson.Empty));
        _result = Result(freshSession.Recover(Arguments(new { operationId = _operation })));
    }

    [Fact] void should_block_opening_partial_state() => _blocked.Message.Contains("PendingOperation", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_require_an_explicit_rollback() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_restore_original_exact_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_restore_original_exact_catalog() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
    [Fact] void should_remove_new_source() => File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
    [Fact] void should_remove_the_verified_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
