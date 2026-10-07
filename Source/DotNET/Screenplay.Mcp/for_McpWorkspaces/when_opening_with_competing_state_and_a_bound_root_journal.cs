// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_opening_with_competing_state_and_a_bound_root_journal : given.a_competing_workspace_state
{
    JsonElement _error;

    void Establish()
    {
        var proposal = Move(Applied, "Models/renamed.play");
        McpRecoveryJournal.Prepare(Root, proposal, new(StateBytes, McpState.Serialize(proposal.Workspace)));
    }

    void Because() => _error = Call("open-workspace").GetProperty("result");

    [Fact] void should_refuse_opening_with_a_pending_operation() => _error.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_name_the_bound_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"Bound root: '{RootPath}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_competing_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_both_state_roots_in_order() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("stateRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([RootPath, ModelRoot]);
    [Fact] void should_identify_the_bound_root_journal() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("pendingRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([RootPath]);
}
