// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_registered_worktree : given.a_worktree_connection
{
    JsonElement _opened;
    void Because() => _opened = Open(WorktreePath);
    [Fact] void should_resolve_the_corresponding_model_root() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(1);
    [Fact] void should_accept_the_model_for_authoring() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_create_identity_state_on_open() => Directory.Exists(Path.Combine(WorktreeModelPath, ".screenplay")).ShouldBeFalse();
}
