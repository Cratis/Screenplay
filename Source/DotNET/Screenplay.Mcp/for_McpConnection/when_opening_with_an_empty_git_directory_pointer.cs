// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_an_empty_git_directory_pointer : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish() => File.WriteAllText(Path.Combine(WorktreePath, ".git"), "gitdir: \n");
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_report_root_change_refusal_for_invalid_path_arguments() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
}
