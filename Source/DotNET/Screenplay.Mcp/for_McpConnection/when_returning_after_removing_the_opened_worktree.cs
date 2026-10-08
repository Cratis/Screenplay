// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_returning_after_removing_the_opened_worktree : given.a_worktree_connection
{
    JsonElement _opened;
    void Establish()
    {
        _ = Open(WorktreePath);
        Git(RepositoryPath, "worktree", "remove", WorktreePath);
    }
    void Because() => _opened = Open(ModelPath);
    [Fact] void should_open_the_configured_root_without_reading_the_removed_root() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(1);
    [Fact] void should_exercise_a_removed_worktree_directory() => Directory.Exists(WorktreePath).ShouldBeFalse();
}
