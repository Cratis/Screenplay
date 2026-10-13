// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaseline;

public class when_recovering_a_first_admission : for_McpDurableState.given.a_durable_workspace
{
    McpRecoveryJournal _journal = null!;

    void Establish()
    {
        var document = ReleaseBaseline.Admit(Original, ReleaseBaselineStore.ReadBytes(null, Original.IdentityCatalog.Application), "v1", DateTimeOffset.UtcNow).Document!;
        var after = ReleaseBaselineStore.Serialize(document);
        _journal = McpRecoveryJournal.Prepare(Root, new McpReleaseBaselineProposal(Original, null, after), new(OriginalState, OriginalState));
        McpManagedFiles.WritePrivate(_journal.ReleaseBaselineStage, after);
        File.Move(_journal.ReleaseBaselineStage, Files.PathFor(ReleaseBaselineStore.FileName));
    }

    void Because() => new McpRecovery(Root, McpRecoveryJournal.Load(Root)!).Rollback();

    [Fact] void should_restore_absence() => Files.Read(ReleaseBaselineStore.FileName).ShouldBeNull();
    [Fact] void should_preserve_identity_state() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_clear_the_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
