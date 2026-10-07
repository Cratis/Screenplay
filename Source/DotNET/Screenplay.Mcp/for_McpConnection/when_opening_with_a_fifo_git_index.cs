// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpConnection.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_with_a_fifo_git_index : given.a_fifo_connection
{
    JsonElement _refused;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RepositoryPath, ".git", "config"), "[core]\nrepositoryformatversion = 0\n");
        var index = Path.Combine(RepositoryPath, ".git", "index");
        File.Delete(index);
        CreateFifo(index);
    }

    async Task Because() => _refused = await OpenWithinDeadline(WorktreePath);

#pragma warning disable CRSPEC0004 // UnixLinkFact derives FactAttribute; xUnit discovers this platform-dependent fact.
    [UnixLinkFact] void should_refuse_a_nonregular_index_as_evidence_of_a_working_tree() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
#pragma warning restore CRSPEC0004
}
