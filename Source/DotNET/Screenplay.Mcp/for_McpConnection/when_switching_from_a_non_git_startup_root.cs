// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_switching_from_a_non_git_startup_root : given.a_worktree_connection
{
    JsonElement _refused;
    void Establish()
    {
        // A filesystem root has no ancestors: it cannot inherit the spec checkout's Git registration.
        // It is used only for root admission, never for reading or writing model files.
        var nonGitRoot = Path.GetPathRoot(RootPath)!;
        Directory.Exists(Path.Combine(nonGitRoot, ".git")).ShouldBeFalse();
        File.Exists(Path.Combine(nonGitRoot, ".git")).ShouldBeFalse();
        Connection = new(new McpTools(new McpRoot(nonGitRoot)));
        Initialize();
    }
    void Because() => _refused = Call("open-workspace", new { path = WorktreeModelPath }).GetProperty("result");
    [Fact] void should_refuse_switching_without_a_configured_git_repository() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    [Fact] void should_explain_the_missing_membership_proof() => _refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("registered worktree of the configured repository");
}
