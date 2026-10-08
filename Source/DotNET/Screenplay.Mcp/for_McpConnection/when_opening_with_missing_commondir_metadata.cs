// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_missing_commondir_metadata : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish()
    {
        var registration = Directory.GetDirectories(Path.Combine(RepositoryPath, ".git", "worktrees")).Single();
        File.Delete(Path.Combine(registration, "commondir"));
    }
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_report_root_change_refusal() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_explain_the_missing_commondir() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("no commondir");
}
