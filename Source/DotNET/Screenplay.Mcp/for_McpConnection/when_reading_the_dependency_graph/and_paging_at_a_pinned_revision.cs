// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_paging_at_a_pinned_revision : given.a_graph_query
{
    JsonElement _next;

    void Because()
    {
        _result = Read(_snapshot, new { view = "order", limit = 1 });
        _next = Read(_snapshot, new { view = "order", limit = 1, offset = 1, expectedSourceRevision = _snapshot.SourceRevision });
    }

    [Fact] void should_pin_the_source_revision() => _result.GetProperty("sourceRevision").GetString().ShouldEqual(_snapshot.SourceRevision);
    [Fact] void should_offer_a_continuation() => _result.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_return_the_requested_offset() => _next.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
}
