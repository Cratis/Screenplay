// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_declarations_are_checked : given.a_graph_query
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    JsonElement _item;
    JsonElement _details;
    JsonElement _defaultEvidence;
    JsonElement _limitedEvidence;
    JsonElement _zeroEvidence;
    JsonElement _maximumEvidence;
    JsonElement _uncertain;

    void Because()
    {
        _snapshot = Snapshot(Source.Replace("module A", "module A\n  depends on B", StringComparison.Ordinal));
        _result = Read(_snapshot, new { view = "declarations" });
        _item = _result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        var arguments = JsonSerializer.SerializeToElement(new { address = "A", kind = "Module", view = "dependencies" });
        _details = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, arguments), _options);
        var repeated = Snapshot(Source.Replace("module A", "module A\n  depends on B", StringComparison.Ordinal)
            .Replace("        from E", string.Join('\n', Enumerable.Repeat("        from E", 25)), StringComparison.Ordinal));
        _defaultEvidence = Read(repeated, new { view = "declarations" }).GetProperty("page").GetProperty("items")[0];
        _limitedEvidence = Read(repeated, new { view = "declarations", evidenceLimit = 1 }).GetProperty("page").GetProperty("items")[0];
        _zeroEvidence = Read(repeated, new { view = "declarations", evidenceLimit = 0 }).GetProperty("page").GetProperty("items")[0];
        _maximumEvidence = Read(repeated, new { view = "declarations", evidenceLimit = 20 }).GetProperty("page").GetProperty("items")[0];
        _uncertain = Read(Snapshot("module A\n  depends on B\n  feature F\n    slice StateView V\n      projection P\n        from E\n        from Other\nmodule B\n  feature G\n    slice StateChange W\n      event E\nmodule C\n  feature H\n    slice StateChange X\n      event E\nmodule D\n  feature I\n    slice StateChange Y\n      event Other"), new { view = "declarations" }).GetProperty("page").GetProperty("items")[0];
    }

    [Fact] void should_page_only_opted_in_containers() => _item.GetProperty("container").GetProperty("address").GetString().ShouldEqual("A");
    [Fact] void should_resolve_the_authored_target() => _item.GetProperty("declarations")[0].GetProperty("resolved").GetString().ShouldEqual("B");
    [Fact] void should_mark_the_declaration_used() => _item.GetProperty("declarations")[0].GetProperty("status").GetString().ShouldEqual("used");
    [Fact] void should_exclude_specification_evidence() => _item.GetProperty("edges").GetArrayLength().ShouldEqual(1);
    [Fact] void should_show_the_covering_declaration() => _item.GetProperty("edges")[0].GetProperty("coveringDeclarations")[0].GetProperty("target").GetString().ShouldEqual("B");
    [Fact] void should_offer_the_authored_dependencies_view() => _details.GetProperty("details").GetProperty("items")[0].GetProperty("target").GetString().ShouldEqual("B");
    [Fact] void should_bound_default_evidence_per_edge() => _defaultEvidence.GetProperty("edges")[0].GetProperty("evidence").GetArrayLength().ShouldEqual(3);
    [Fact] void should_honor_the_requested_evidence_limit() => _limitedEvidence.GetProperty("edges")[0].GetProperty("evidence").GetArrayLength().ShouldEqual(1);
    [Fact] void should_allow_count_only_edges() => _zeroEvidence.GetProperty("edges")[0].GetProperty("evidence").GetArrayLength().ShouldEqual(0);
    [Fact] void should_allow_the_maximum_evidence_limit() => _maximumEvidence.GetProperty("edges")[0].GetProperty("evidence").GetArrayLength().ShouldEqual(20);
    [Fact] void should_keep_complete_evidence_counts() => _limitedEvidence.GetProperty("edges")[0].GetProperty("evidenceCount").GetInt32().ShouldEqual(25);
    [Fact] void should_signal_truncated_evidence() => _limitedEvidence.GetProperty("edges")[0].GetProperty("evidenceTruncated").GetBoolean().ShouldBeTrue();
    [Fact] void should_signal_complete_evidence() => _item.GetProperty("edges")[0].GetProperty("evidenceTruncated").GetBoolean().ShouldBeFalse();
    [Fact] void should_retain_evidence_locations() => _limitedEvidence.GetProperty("edges")[0].GetProperty("evidence")[0].GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(6);
    [Fact] void should_project_provisional_declarations() => _uncertain.GetProperty("declarations")[0].GetProperty("status").GetString().ShouldEqual("provisional");
    [Fact] void should_project_provisional_edges() => _uncertain.GetProperty("edges")[0].GetProperty("status").GetString().ShouldEqual("provisional");
    [Fact] void should_retain_ambiguous_alternatives() => _uncertain.GetProperty("edges")[0].GetProperty("evidence")[0].GetProperty("alternatives")[0].GetProperty("address").GetString().ShouldEqual("C.H.X");
    [Fact] void should_project_undeclared_edges() => _uncertain.GetProperty("edges")[1].GetProperty("status").GetString().ShouldEqual("undeclared");
    [Fact] void should_keep_undeclared_edges_without_covering_declarations() => _uncertain.GetProperty("edges")[1].GetProperty("coveringDeclarations").GetArrayLength().ShouldEqual(0);
}
