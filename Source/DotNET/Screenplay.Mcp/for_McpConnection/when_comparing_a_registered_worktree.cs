// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_comparing_a_registered_worktree : given.a_worktree_connection
{
    JsonElement _opened;
    JsonElement _result;
    JsonElement _current;

    void Establish() => _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");

    void Because()
    {
        _result = Call("semantic-diff", new { before = new { workspace = "active" }, after = new { path = WorktreePath } }).GetProperty("result");
        _current = Call("read-workspace", new { expectedRevision = _opened.GetProperty("revision").GetString() }).GetProperty("result");
    }

    [Fact] void should_admit_the_registered_worktrees_model() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_compare_the_same_model() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    [Fact] void should_not_rebind_the_workspace() => _current.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_create_worktree_metadata() => Directory.Exists(Path.Combine(WorktreeModelPath, ".screenplay")).ShouldBeFalse();
}
