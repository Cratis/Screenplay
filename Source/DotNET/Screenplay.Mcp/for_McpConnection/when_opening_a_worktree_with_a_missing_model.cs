// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_worktree_with_a_missing_model : given.a_worktree_connection
{
    JsonElement _opened;

    void Establish() => Directory.Delete(WorktreeModelPath, true);

    void Because() => _opened = Open(WorktreeModelPath);

    [Fact] void should_open_an_empty_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_create_the_worktree_model_folder() => Directory.Exists(WorktreeModelPath).ShouldBeFalse();
}
