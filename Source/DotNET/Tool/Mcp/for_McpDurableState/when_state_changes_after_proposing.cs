// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_state_changes_after_proposing : given.a_durable_workspace
{
    Exception _error = null!;

    void Establish() => File.WriteAllText(Files.PathFor(McpState.FileName), "external-state");
    void Because() => _error = Catch.Exception(() => new McpDisk(Root).Apply(Proposal, new(OriginalState, McpState.Serialize(Proposal.Workspace))));

    [Fact] void should_detect_identity_state_drift() => _error.Message.Contains("IdentityStateDrift", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_mutate_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_not_create_a_pending_operation() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
