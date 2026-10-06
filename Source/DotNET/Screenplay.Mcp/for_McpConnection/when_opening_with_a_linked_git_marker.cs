// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

using Cratis.Screenplay.Mcp.for_McpConnection.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_linked_git_marker : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish()
    {
        var marker = Path.Combine(WorktreePath, ".git");
        File.Move(marker, marker + "-original");
        File.CreateSymbolicLink(marker, marker + "-original");
    }
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [UnixLinkFact] void should_report_root_change_refusal_for_a_linked_marker() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [UnixLinkFact] void should_explain_the_link_refusal() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("Symbolic links and reparse points are not admitted");
}
