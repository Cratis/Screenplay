// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_paging_a_trace : given.a_lineage_model
{
    const string Title = "Lending.Catalog.BookCatalog.CatalogEntry.title";

    JsonElement _first;
    JsonElement _second;
    JsonElement _unpinned;
    JsonElement _full;

    void Establish() => Write(File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play")));

    void Because()
    {
        _first = Trace(Title, limit: 2);
        _second = Call("trace-lineage", new { address = Title, offset = 2, limit = 2, expectedSourceRevision = _first.GetProperty("sourceRevision").GetString() }).GetProperty("result").GetProperty("structuredContent");
        _unpinned = Call("trace-lineage", new { address = Title, offset = 2 });
        _full = Trace(Title);
    }

    [Fact] void should_bound_the_first_page() => Hops(_first).Length.ShouldEqual(2);
    [Fact] void should_offer_a_continuation() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(2);
    [Fact] void should_continue_where_the_first_page_ended() => Hops(_second).Select(hop => hop.GetProperty("id").GetInt32()).ShouldEqual([.. Hops(_full).Skip(2).Take(2).Select(hop => hop.GetProperty("id").GetInt32())]);
    [Fact] void should_repeat_the_whole_trace_summary_on_every_page() => _second.GetProperty("hopCount").GetInt32().ShouldEqual(_full.GetProperty("hopCount").GetInt32());
    [Fact] void should_require_the_source_revision_on_continuation() => _unpinned.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
}
