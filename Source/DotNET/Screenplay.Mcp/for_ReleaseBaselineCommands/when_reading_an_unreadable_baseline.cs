// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_reading_an_unreadable_baseline : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Establish() => File.WriteAllText(BaselinePath, "<<<<<<< main");

    void Because() => _outcome = ReleaseBaselineCommands.Check(RootPath);

    [Fact] void should_report_unreadable() => _outcome.ExitCode.ShouldEqual(2);
    [Fact] void should_name_the_refusal() => _outcome.FailureKind.ShouldEqual("UnreadableReleaseBaseline");
    [Fact] void should_explain_the_refusal() => _outcome.Reason!.ShouldContain("UnreadableReleaseBaseline:");
    [Fact] void should_not_repair_the_file() => File.ReadAllText(BaselinePath).ShouldEqual("<<<<<<< main");
}
