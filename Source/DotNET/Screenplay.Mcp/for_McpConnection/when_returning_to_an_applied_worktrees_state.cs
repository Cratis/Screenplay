// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_returning_to_an_applied_worktrees_state : given.a_worktree_connection
{
    byte[] _persisted;
    JsonElement _state;
    void Establish()
    {
        var opened = Open(WorktreeModelPath);
        var proposalId = ProposeMove(opened).GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
        ApplyMove(opened, proposalId).GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeTrue();
        _persisted = File.ReadAllBytes(Path.Combine(WorktreeModelPath, ".screenplay", "identities.json"));
        _ = Open(ModelPath);
    }
    void Because()
    {
        _ = Open(WorktreePath);
        _state = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }
    [Fact] void should_reload_the_worktrees_persisted_identity_state() => _state.GetProperty("exists").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_exact_applied_identity_state() => File.ReadAllBytes(Path.Combine(WorktreeModelPath, ".screenplay", "identities.json")).ShouldEqual(_persisted);
}
