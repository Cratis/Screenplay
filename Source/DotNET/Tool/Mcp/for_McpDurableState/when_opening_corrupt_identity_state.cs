// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_opening_corrupt_identity_state : given.a_durable_workspace
{
    Exception _error = null!;

    void Establish() => File.WriteAllText(Files.PathFor(McpState.FileName), "corrupt-state");
    void Because() => _error = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_not_bootstrap_new_identities() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_explain_the_conflict() => _error.Message.Contains("IdentityStateConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_corrupt_state_for_recovery() => File.ReadAllText(Files.PathFor(McpState.FileName)).ShouldEqual("corrupt-state");
}
