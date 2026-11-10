// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_bounding_agent_exposure_pages : Specification
{
    JsonElement _result;

    void Because()
    {
        var rows = Enumerable.Range(0, 64).Select(index => new AgentExposureChange(AgentOperationKind.Query, $"{index:D3}.{new string('x', 70_000)}", AgentExposureChangeKind.Unchanged, "Unchanged", AgentScope.Read, AgentScope.Read, null)).ToImmutableArray();
        var exposure = new AgentExposureComparison(AgentExposureCoverage.Complete, rows);
        _result = JsonSerializer.SerializeToElement(McpAgentExposureDiff.Read(exposure, JsonSerializer.SerializeToElement(new { version = 2, limit = 200 }), "revision", "before", "after"), McpJson.Options);
    }

    [Fact] void should_stay_within_the_response_byte_cap() => JsonSerializer.SerializeToUtf8Bytes(_result, McpJson.Options).Length.ShouldBeLessThanOrEqual(McpJson.MaximumStructuredResponseBytes);
    [Fact] void should_shorten_the_page_before_the_limit() => _result.GetProperty("page").GetProperty("items").GetArrayLength().ShouldBeLessThan(64);
    [Fact] void should_advance_by_the_actual_row_count() => _result.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(_result.GetProperty("page").GetProperty("items").GetArrayLength());
    [Fact] void should_bind_the_page_to_the_snapshot() => _result.GetProperty("page").GetProperty("revision").GetString().ShouldEqual("revision");
    [Fact] void should_keep_the_requested_version_when_byte_bounded() => _result.GetProperty("version").GetInt32().ShouldEqual(2);
    [Fact] void should_keep_coverage_when_byte_bounded() => _result.GetProperty("coverage").GetString().ShouldEqual("Complete");
}
