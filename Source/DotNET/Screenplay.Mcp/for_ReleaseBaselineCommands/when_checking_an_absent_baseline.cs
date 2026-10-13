// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_checking_an_absent_baseline : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Because() => _outcome = ReleaseBaselineCommands.Check(RootPath);

    [Fact] void should_pass() => _outcome.ExitCode.ShouldEqual(0);
    [Fact] void should_have_no_violations() => _outcome.Violations.ShouldBeEmpty();
    [Fact] void should_not_create_a_baseline() => File.Exists(BaselinePath).ShouldBeFalse();
    [Fact] void should_preserve_identities() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
}
