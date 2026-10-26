// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_indexing_event_targets : given.a_connection
{
    JsonElement[] _results = [];

    void Establish()
    {
        foreach (var source in event_target_applications.CatalogSources()) File.WriteAllText(Path.Combine(RootPath, source.Key), source.Value);
        Initialize();
    }

    void Because() => _results = [.. event_target_applications.Catalog.Select(value => Call("dependencies", new { address = "Merchandising.Publication." + value.Slice + "." + value.Projection, kind = "Projection", direction = "outgoing" }).GetProperty("result").GetProperty("structuredContent"))];

    [Fact] void should_resolve_each_build_to_the_explicit_event() => Builds().Select(edge => edge.GetProperty("resolution").GetString()).ShouldEqual(["resolved", "resolved", "resolved"]);
    [Fact] void should_request_events_instead_of_readmodels() => Builds().Select(edge => edge.GetProperty("reference").GetProperty("kinds")[0].GetString()).ShouldEqual(["Event", "Event", "Event"]);
    [Fact] void should_return_the_exact_catalog_candidate_addresses() => Builds().Select(edge => string.Join(',', edge.GetProperty("targets").EnumerateArray().Select(target => target.GetProperty("address").GetString()))).ShouldEqual(event_target_applications.Catalog.Select(value => "Merchandising.Publication." + value.Slice + "." + value.Event));
    [Fact] void should_keep_the_projection_owner() => Builds().Select(edge => edge.GetProperty("reference").GetProperty("owner").GetProperty("address").GetString()).ShouldEqual(event_target_applications.Catalog.Select(value => "Merchandising.Publication." + value.Slice + "." + value.Projection));
    [Fact] void should_keep_the_source_location() => Builds().Select(edge => edge.GetProperty("reference").GetProperty("location").GetProperty("path").GetString()).ShouldEqual(["a-targets.play", "a-targets.play", "a-targets.play"]);
    [Fact] void should_keep_genuine_event_input_candidates() => _results.SelectMany(result => result.GetProperty("page").GetProperty("items").EnumerateArray()).Where(edge => edge.GetProperty("reference").GetProperty("role").GetString() == "from").Select(edge => edge.GetProperty("resolution").GetString()).ShouldEqual(["resolved", "resolved", "resolved"]);

    IEnumerable<JsonElement> Builds() => _results.SelectMany(result => result.GetProperty("page").GetProperty("items").EnumerateArray()).Where(edge => edge.GetProperty("reference").GetProperty("role").GetString() == "builds");
}
