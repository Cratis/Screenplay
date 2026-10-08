// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_malformed_git_pointer : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish() => File.WriteAllText(Path.Combine(WorktreePath, ".git"), "not a gitdir pointer\n");
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_report_root_change_refusal() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_refuse_membership_rather_than_a_missing_model() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
}
