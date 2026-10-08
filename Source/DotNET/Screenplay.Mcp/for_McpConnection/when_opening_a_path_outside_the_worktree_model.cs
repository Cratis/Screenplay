// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_path_outside_the_worktree_model : given.a_worktree_connection
{
    JsonElement _refused;
    string _escaped;
    void Establish()
    {
        _escaped = Path.Combine(WorktreeModelPath, "..", "other");
        Directory.CreateDirectory(_escaped);
    }
    void Because() => _refused = Call("open-workspace", new { path = _escaped }).GetProperty("result");
    [Fact] void should_refuse_a_different_relative_model_root() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
}
