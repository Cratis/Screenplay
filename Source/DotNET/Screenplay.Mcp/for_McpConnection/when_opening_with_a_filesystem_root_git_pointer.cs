// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_filesystem_root_git_pointer : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish() => File.WriteAllText(Path.Combine(WorktreePath, ".git"), $"gitdir: {Path.GetPathRoot(RootPath)}\n");
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_refuse_a_git_directory_without_a_parent() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_explain_the_invalid_registration() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("beneath a filesystem root");
}
