// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_large_git_configuration : given.a_worktree_connection
{
    JsonElement _linked;
    JsonElement _main;

    void Establish()
    {
        var path = Path.Combine(RepositoryPath, ".git", "config");
        var branches = string.Concat(Enumerable.Range(0, 307).Select(index => $"[branch \"feature/{index}\"]\n\tremote = origin\n\tmerge = refs/heads/feature/{index}\n\tgithub-pr-owner-number = Cratis#Screenplay#{index}\n"));
        var config = File.ReadAllText(path) + branches;
        Encoding.UTF8.GetByteCount(config).ShouldBeGreaterThan(8192);
        File.WriteAllText(path, config);
    }

    void Because()
    {
        _linked = Open(WorktreePath);
        _main = Open(RepositoryPath);
    }

    [Fact] void should_switch_from_the_main_checkout_to_the_linked_worktree() => _linked.GetProperty("documentCount").GetInt32().ShouldEqual(1);
    [Fact] void should_switch_back_to_the_main_checkout() => _main.GetProperty("documentCount").GetInt32().ShouldEqual(1);
}
