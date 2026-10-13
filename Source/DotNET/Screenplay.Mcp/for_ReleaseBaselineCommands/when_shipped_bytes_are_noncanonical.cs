// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_shipped_bytes_are_noncanonical : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;
    string _shippedPath = null!;

    void Establish()
    {
        _shippedPath = Path.Combine(RootPath, "shipped.json");
        File.WriteAllText(_shippedPath, "{}");
    }

    void Because() => _outcome = ReleaseBaselineCommands.Admit(RootPath, "v1", _shippedPath);

    [Fact] void should_refuse() => _outcome.ExitCode.ShouldEqual(2);
    [Fact] void should_explain_the_failure() => _outcome.Reason.ShouldNotBeNull();
    [Fact] void should_not_write_a_baseline() => File.Exists(BaselinePath).ShouldBeFalse();
}
