// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_a_project_root_recovery_journal : given.a_persisted_nested_model
{
    McpRecoveryJournal _journal = null!;
    byte[] _marker = [];
    JsonElement _status;
    Exception? _failure;

    void Establish()
    {
        // A first apply may be interrupted before identities.json is installed: the journal alone owns the root.
        File.Delete(Files.PathFor(McpState.FileName));
        var proposal = Move(Applied, "Models/renamed.play");
        _journal = McpRecoveryJournal.Prepare(Root, proposal, new(null, McpState.Serialize(proposal.Workspace)));
        _marker = Files.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        _failure = Catch.Exception(() => Workspaces.Open(McpJson.Empty));
        _status = Result(Workspaces.State(McpJson.Empty));
    }

    [Fact] void should_bind_the_project_root() => Workspaces.ReadRoot().DirectoryPath.TrimEnd(Path.DirectorySeparatorChar).ShouldEqual(RootPath);
    [Fact] void should_block_opening_until_explicit_recovery() => ((McpFailure)_failure!).FailureKind.ShouldEqual("PendingOperation");
    [Fact] void should_report_the_pending_operation() => _status.GetProperty("recovery").GetProperty("pending").GetBoolean().ShouldBeTrue();
    [Fact] void should_offer_recovery_of_the_original_operation() => _status.GetProperty("recovery").GetProperty("operationId").GetString().ShouldEqual(_journal.Record.OperationId);
    [Fact] void should_offer_rollback() => _status.GetProperty("recovery").GetProperty("canRollback").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_journal() => Files.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
    [Fact] void should_not_install_or_migrate_identities() => Files.Read(McpState.FileName).ShouldBeNull();
}
