// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_per_worktree_git_configuration : given.a_worktree_connection
{
    [Theory]
    [InlineData("[extensions]\nworktreeConfig = true\n")]
    [InlineData("[EXTENSIONS]\nWORKTREECONFIG = TRUE\n")]
    [InlineData("[extensions]\nworktreeConfig = yes\n")]
    [InlineData("[extensions]\nworktreeConfig = on\n")]
    [InlineData("[extensions]\nworktreeConfig = 1\n")]
    [InlineData("[extensions]\nworktreeConfig = unknown\n")]
    [InlineData("[extensions]\nworktreeConfig\n")]
    [InlineData("[extensions]\nworktreeConfig = false\nworktreeConfig = true\n")]
    public void should_refuse_enabled_or_unverifiable_worktree_configuration(string extension)
    {
        File.Exists(Path.Combine(RepositoryPath, ".git", "index")).ShouldBeTrue();
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nrepositoryformatversion = 0\n" + extension);
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
    }

    [Theory]
    [InlineData("[core]\nrepositoryformatversion = 0\n[extensions]\nworktreeConfig = true\n")]
    [InlineData("[core]\nrepositoryformatversion = 0\n")]
    [InlineData("[core]\nbare = false\n[extensions]\nworktreeConfig = false\n")]
    public void should_refuse_an_existing_worktree_configuration_even_if_disabled(string config)
    {
        File.Exists(Path.Combine(RepositoryPath, ".git", "index")).ShouldBeTrue();
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config.worktree"), "[core]\nbare = true\n");
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("Per-worktree configuration is not supported.");
    }
}
