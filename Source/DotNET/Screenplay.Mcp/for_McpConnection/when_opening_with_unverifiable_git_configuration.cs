// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_unverifiable_git_configuration : given.a_worktree_connection
{
    [Theory]
    [InlineData("[core]\nrepositoryformatversion = 0\n")]
    [InlineData("[core]\nbare = false\nbare = true\n")]
    [InlineData("[core]\nbare = false\nbare = false\n")]
    [InlineData("[core]\nbare = false\n[core]\nbare = false\n")]
    [InlineData("[core]\nbare = unknown\n")]
    [InlineData("[core]\nbare\n")]
    [InlineData("[core]\nbare = false\n[include]\npath = other-config\n")]
    [InlineData("[core]\nother = value\\\nbare = false\n")]
    [InlineData("[core\nbare = false\n")]
    [InlineData("[core]\nbare = false\0\n")]
    public void should_refuse_ambiguous_or_unreadable_bare_status(string config)
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    }

    [Fact]
    void should_refuse_a_missing_configuration()
    {
        File.Delete(Path.Combine(RepositoryPath, ".git", "config"));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    }

    [Fact]
    void should_refuse_an_oversized_configuration()
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nbare = false\n#" + new string('x', 8192));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    }
}
