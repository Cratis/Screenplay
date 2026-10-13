// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_admitting : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Because() => _outcome = ReleaseBaselineCommands.Admit(RootPath, "v1");

    [Fact] void should_pass() => _outcome.ExitCode.ShouldEqual(0);
    [Fact] void should_report_the_written_entry_count() => _outcome.AdmittedEntryCount.ShouldEqual(1);
    [Fact] void should_report_the_written_revision() => _outcome.BaselineRevision.ShouldEqual(ReleaseBaselineStore.Revision(File.ReadAllBytes(BaselinePath)));
    [Fact] void should_leave_no_pending_operation() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_preserve_identities() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_preserve_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_leave_a_clean_baseline() => ReleaseBaselineCommands.Check(RootPath).ExitCode.ShouldEqual(0);
}
