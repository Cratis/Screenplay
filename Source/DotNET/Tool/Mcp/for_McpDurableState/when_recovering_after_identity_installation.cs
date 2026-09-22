// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_recovering_after_identity_installation : given.a_durable_workspace
{
    string _operation = string.Empty;
    JsonElement _result;
    string _access = string.Empty;

    void Establish()
    {
        _access = McpRecoveryAccess.Capture("application.play", Path.Combine(RootPath, "application.play")).Rules;
        var journal = Prepare();
        _operation = journal.Record.OperationId;
        Interrupt(journal, installState: true);
        File.Delete(journal.Changes.Single().Backup);
    }

    void Because() => _result = Result(new McpWorkspaces(new McpRoot(RootPath)).Recover(Arguments(new { operationId = _operation })));

    [Fact] void should_recover_even_if_cleanup_already_removed_a_backup() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_restore_exact_source_from_the_canonical_journal() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_restore_original_access_rules() => McpRecoveryAccess.Capture("application.play", Path.Combine(RootPath, "application.play")).Rules.ShouldEqual(_access);
    [Fact] void should_restore_exact_original_identity_state() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
    [Fact] void should_remove_the_marker_after_verification() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
