// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_recovery_finds_competing_source : given.a_durable_workspace
{
    McpRecoveryJournal _journal = null!;
    JsonElement _result;
    byte[] _marker = [];

    void Establish()
    {
        _journal = Prepare();
        Interrupt(_journal, installState: true);
        _marker = Files.Read(McpRecoveryJournal.FileName);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "competing external bytes");
    }

    void Because() => _result = Result(new McpWorkspaces(Root).Recover(Arguments(new { operationId = _journal.Record.OperationId })));

    [Fact] void should_refuse_the_entire_recovery() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_preserve_the_external_file() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual("competing external bytes");
    [Fact] void should_not_remove_the_known_installed_file_either() => File.ReadAllText(Path.Combine(RootPath, "renamed.play")).ShouldEqual(Source);
    [Fact] void should_not_restore_only_the_catalog() => McpManagedFiles.Equal(Files.Read(McpState.FileName), _journal.Record.AfterState).ShouldBeTrue();
    [Fact] void should_retain_the_exact_marker() => McpManagedFiles.Equal(Files.Read(McpRecoveryJournal.FileName), _marker).ShouldBeTrue();
    [Fact] void should_retain_the_backup() => File.Exists(_journal.Changes.Single().Backup).ShouldBeTrue();
}
