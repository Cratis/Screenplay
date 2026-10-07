// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_unverifiable_git_configuration : given.a_worktree_connection
{
    [Theory]
    [InlineData("[core]\nbare = false\nbare = true\n")]
    [InlineData("[core]\nbare = false\nbare = false\n")]
    [InlineData("[core]\nbare = false\n[core]\nbare = false\n")]
    [InlineData("[core]\nbare = unknown\n")]
    [InlineData("[core]\nbare\n")]
    [InlineData("[core]\nbare = false\n[include]\npath = other-config\n")]
    [InlineData("[core]\nbare = false\n[includeIf \"gitdir:work\"]\npath = other-config\n")]
    [InlineData("[core]\nother = value\\\nbare = false\n")]
    [InlineData("[core\nbare = false\n")]
    [InlineData("[core]\nbare = false\0\n")]
    [InlineData("[core] bare = true # ]\n")]
    [InlineData("[include] path = x ;]\n")]
    [InlineData("[include] ;]\npath = x\n")]
    [InlineData("[includeIf \"gitdir:work\"] # ]\npath = x\n")]
    [InlineData("[core \"unterminated]\nbare = false\n")]
    public void should_refuse_ambiguous_or_unreadable_bare_status(string config)
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
    }

    [Fact]
    void should_refuse_a_missing_configuration()
    {
        File.Delete(Path.Combine(RepositoryPath, ".git", "config"));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
    }

    [Fact]
    void should_refuse_an_oversized_configuration()
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nbare = false\n" + new string('\n', 1024 * 1024));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("total limit");
    }

    [Fact]
    void should_refuse_an_oversized_configuration_line()
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nbare = false\n#" + new string('x', 64 * 1024));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("line exceeds");
    }

    [Fact]
    void should_refuse_absent_bare_status_without_an_index()
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nrepositoryformatversion = 0\n");
        File.Delete(Path.Combine(RepositoryPath, ".git", "index"));
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain("no working tree index exists");
    }

    [Theory]
    [InlineData("\uFEFF[core]\nbare = true\n")]
    [InlineData("[CORE]\nBARE = TRUE\n")]
    [InlineData("[core] ;]\nbare = true\n")]
    public void should_not_use_the_index_to_override_explicit_bare_status(string config)
    {
        File.Exists(Path.Combine(RepositoryPath, ".git", "index")).ShouldBeTrue();
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
    }

    [Theory]
    [InlineData("[core]\nbare = false\n")]
    [InlineData("[include]\npath = other-config\n")]
    [InlineData("[includeIf \"gitdir:work\"]\npath = other-config\n")]
    public void should_check_the_end_of_the_configuration(string suffix)
    {
        var config = "[core]\nbare = false\n#" + new string('x', 9000) + "\n" + suffix;
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        var refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
        refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
        refused.GetProperty("structuredContent").GetProperty("message").GetString()!.ShouldContain($"Cannot determine whether {RepositoryPath} is a bare repository:");
    }
}
