// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_returning_to_the_configured_roots_state : given.a_worktree_connection
{
    JsonElement _state;
    void Establish()
    {
        var opened = Open(WorktreeModelPath);
        var proposalId = ProposeMove(opened).GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
        ApplyMove(opened, proposalId).GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeTrue();
    }
    void Because()
    {
        _ = Open(ModelPath);
        _state = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }
    [Fact] void should_load_the_configured_roots_own_absent_identity_state() => _state.GetProperty("exists").GetBoolean().ShouldBeFalse();
    [Fact] void should_preserve_the_worktrees_applied_identity_state() => File.Exists(Path.Combine(WorktreeModelPath, ".screenplay", "identities.json")).ShouldBeTrue();
}
