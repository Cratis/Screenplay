// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaseline;

public class when_recovering_an_interrupted_admission : for_McpDurableState.given.a_durable_workspace
{
    McpRecoveryJournal _journal = null!;
    byte[] _before = [];
    byte[] _after = [];

    void Establish()
    {
        var absent = ReleaseBaselineStore.ReadBytes(null, Original.IdentityCatalog.Application);
        var document = ReleaseBaseline.Admit(Original, absent, "v1", DateTimeOffset.UtcNow).Document!;
        _before = ReleaseBaselineStore.Serialize(document);
        _after = ReleaseBaselineStore.Serialize(document with { Entries = [.. document.Entries.Select(entry => entry with { AdmittedIn = "changed" })] });
        McpManagedFiles.WritePrivate(Files.PathFor(ReleaseBaselineStore.FileName), _before);
        _journal = McpRecoveryJournal.Prepare(Root, new McpReleaseBaselineProposal(Original, _before, _after), new(OriginalState, OriginalState));
        McpManagedFiles.WritePrivate(_journal.ReleaseBaselineStage, _after);
        File.Move(Files.PathFor(ReleaseBaselineStore.FileName), _journal.ReleaseBaselineBackup);
        File.Move(_journal.ReleaseBaselineStage, Files.PathFor(ReleaseBaselineStore.FileName));
    }

    void Because() => new McpRecovery(Root, McpRecoveryJournal.Load(Root)!).Rollback();

    [Fact] void should_restore_exact_baseline_bytes() => Files.Read(ReleaseBaselineStore.FileName).ShouldEqual(_before);
    [Fact] void should_leave_identities_unchanged() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_preserve_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_clear_the_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_remove_the_backup() => File.Exists(_journal.ReleaseBaselineBackup).ShouldBeFalse();
    [Fact] void should_use_the_new_envelope_version() => _journal.Record.Version.ShouldEqual(3);
}
