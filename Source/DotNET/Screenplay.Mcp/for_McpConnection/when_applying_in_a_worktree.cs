// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_applying_in_a_worktree : given.a_worktree_connection
{
    JsonElement _opened;
    JsonElement _applied;
    string _proposalId;
    void Establish()
    {
        _opened = Open(WorktreeModelPath);
        _proposalId = ProposeMove(_opened).GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
    }
    void Because() => _applied = ApplyMove(_opened, _proposalId);
    [Fact] void should_apply_successfully() => _applied.GetProperty("structuredContent").GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_write_only_in_the_opened_root() => File.Exists(Path.Combine(WorktreeModelPath, "renamed.play")).ShouldBeTrue();
    [Fact] void should_leave_the_configured_roots_source_untouched() => File.ReadAllText(Path.Combine(ModelPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_persist_identity_state_only_in_the_worktree() => File.Exists(Path.Combine(WorktreeModelPath, ".screenplay", "identities.json")).ShouldBeTrue();
    [Fact] void should_keep_the_configured_roots_state_absent() => File.Exists(Path.Combine(ModelPath, ".screenplay", "identities.json")).ShouldBeFalse();
}
