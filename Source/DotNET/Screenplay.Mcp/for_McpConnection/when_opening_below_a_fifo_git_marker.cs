// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpConnection.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_below_a_fifo_git_marker : given.a_fifo_connection
{
    string _requested;
    JsonElement _refused;

    void Establish()
    {
        var parent = Path.Combine(RootPath, "fifo-ancestor");
        CreateModel(parent);
        _requested = Path.Combine(parent, ".cratis", "screenplay");
        CreateFifo(Path.Combine(parent, ".git"));
    }

    async Task Because() => _refused = await OpenWithinDeadline(_requested);

#pragma warning disable CRSPEC0004 // UnixLinkFact derives FactAttribute; xUnit discovers this platform-dependent fact.
    [UnixLinkFact] void should_refuse_promptly_without_opening_the_ancestor_fifo() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("RootChangeRefused");
#pragma warning restore CRSPEC0004
}
