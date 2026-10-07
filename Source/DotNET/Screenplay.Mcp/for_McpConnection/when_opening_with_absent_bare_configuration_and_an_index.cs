// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_absent_bare_configuration_and_an_index : given.a_worktree_connection
{
    [Theory]
    [InlineData("[core]\nrepositoryformatversion = 0\nfilemode = true\n")]
    [InlineData("[branch \"main\"]\nremote = origin\n")]
    [InlineData("")]
    public void should_switch_between_the_main_checkout_and_its_worktree(string config)
    {
        File.Exists(Path.Combine(RepositoryPath, ".git", "index")).ShouldBeTrue();
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        Open(WorktreePath).GetProperty("documentCount").GetInt32().ShouldEqual(1);
        Open(RepositoryPath).GetProperty("documentCount").GetInt32().ShouldEqual(1);
    }
}
