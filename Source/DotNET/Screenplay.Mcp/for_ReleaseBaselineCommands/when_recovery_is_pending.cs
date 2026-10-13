// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_recovery_is_pending : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Establish() => File.WriteAllText(Files.PathFor(McpRecoveryJournal.FileName), "pending");

    void Because() => _outcome = ReleaseBaselineCommands.Admit(RootPath, "v1");

    [Fact] void should_refuse_admission() => _outcome.ExitCode.ShouldEqual(2);
    [Fact] void should_name_the_pending_operation() => _outcome.FailureKind.ShouldEqual("PendingOperation");
    [Fact] void should_explain_recovery() => _outcome.Reason!.ShouldContain("recover-workspace");
    [Fact] void should_preserve_the_marker() => File.ReadAllText(Files.PathFor(McpRecoveryJournal.FileName)).ShouldEqual("pending");
    [Fact] void should_not_write_a_baseline() => File.Exists(BaselinePath).ShouldBeFalse();
    [Fact] void should_not_report_an_admitted_count() => _outcome.AdmittedEntryCount.ShouldBeNull();
}
