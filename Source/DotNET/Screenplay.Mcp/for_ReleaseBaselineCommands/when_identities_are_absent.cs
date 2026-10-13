// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_identities_are_absent : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Establish() => File.Delete(Files.PathFor(McpState.FileName));

    void Because() => _outcome = ReleaseBaselineCommands.Admit(RootPath, "v1");

    [Fact] void should_refuse() => _outcome.ExitCode.ShouldEqual(2);
    [Fact] void should_require_persisted_identities() => _outcome.FailureKind.ShouldEqual("PersistedIdentitiesRequired");
    [Fact] void should_not_create_identities() => Files.Read(McpState.FileName).ShouldBeNull();
    [Fact] void should_not_write_a_baseline() => File.Exists(BaselinePath).ShouldBeFalse();
}
