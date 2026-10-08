// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_switching_from_a_worktree_startup_root : given.a_worktree_connection
{
    JsonElement _opened;
    void Establish()
    {
        Connection = new(new McpTools(new McpRoot(WorktreeModelPath)));
        Initialize();
    }
    void Because() => _opened = Open(RepositoryPath);
    [Fact] void should_admit_the_main_checkout_of_the_shared_repository() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(1);
}
