// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_worktree_without_the_model_folder : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish() => Directory.Delete(WorktreeModelPath, recursive: true);
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_report_root_change_refusal() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_name_the_missing_relative_model_folder() => _refused.GetProperty("structuredContent").GetProperty("message").GetString().ShouldEqual($"The worktree has no '{Path.Combine(".cratis", "screenplay")}' model folder.");
}
