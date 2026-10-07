// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_levels_are_mixed : given.a_graph_query
{
    JsonElement _item;

    void Because()
    {
        _result = Read(_snapshot, new { from = "feature", to = "module", includeTestOnly = true, evidenceLimit = 1 });
        _item = _result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
    }

    [Fact] void should_use_the_feature_address() => _item.GetProperty("source").GetProperty("address").GetString().ShouldEqual("A.F");
    [Fact] void should_count_test_only_references() => _item.GetProperty("references").GetInt32().ShouldEqual(2);
    [Fact] void should_classify_the_specification_reference() => _item.GetProperty("byKind").GetProperty("verifiedWith").GetInt32().ShouldEqual(1);
    [Fact] void should_keep_uncapped_evidence_count() => _item.GetProperty("evidenceCount").GetInt32().ShouldEqual(2);
    [Fact] void should_report_truncation() => _item.GetProperty("evidenceTruncated").GetBoolean().ShouldBeTrue();
    [Fact] void should_bound_the_evidence() => _item.GetProperty("evidence").GetArrayLength().ShouldEqual(1);
}
