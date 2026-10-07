// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_supported_git_section_header : given.a_worktree_connection
{
    [Theory]
    [InlineData("[core]\r\nbare = false\r\n")]
    [InlineData("[core] # trailing comment ]\nbare = false\n")]
    [InlineData("[core] ;]\nbare = false\n")]
    [InlineData("[CORE]\nBARE = FALSE\n")]
    [InlineData(" \t\v\f[core]\r\n \tbare \t=\v\ffalse\r\n")]
    [InlineData("[core]\nbare = false\n[extensions]\nworktreeConfig = false\n")]
    [InlineData("[core]\nbare = false\n[EXTENSIONS]\nWORKTREECONFIG = NO\n")]
    [InlineData("[core]\nbare = false\n[extensions]\nworktreeConfig = off\n")]
    [InlineData("[core]\nbare = false\n[extensions]\nworktreeConfig = 0\n")]
    [InlineData("[core]\nbare = false\n[extensions]\nworktreeConfig =\n")]
    public void should_switch_between_the_main_checkout_and_its_worktree(string config)
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), config);
        Open(WorktreePath).GetProperty("documentCount").GetInt32().ShouldEqual(1);
        Open(RepositoryPath).GetProperty("documentCount").GetInt32().ShouldEqual(1);
    }
}
