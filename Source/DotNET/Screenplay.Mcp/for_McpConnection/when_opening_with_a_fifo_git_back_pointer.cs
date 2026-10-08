// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpConnection.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_fifo_git_back_pointer : given.a_fifo_connection
{
    JsonElement _refused;

    void Establish()
    {
        var path = Path.Combine(RepositoryPath, ".git", "worktrees", "worktree", "gitdir");
        File.Delete(path);
        CreateFifo(path);
    }

    async Task Because() => _refused = await OpenWithinDeadline(WorktreePath);

#pragma warning disable CRSPEC0004 // UnixLinkFact derives FactAttribute; xUnit discovers this platform-dependent fact.
    [UnixLinkFact] void should_refuse_promptly_without_opening_the_fifo() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
#pragma warning restore CRSPEC0004
}
