// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_agent_exposure_compilation_is_unavailable : given.an_agent_exposure_comparison
{
    JsonElement _unknown;

    void Because()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents.Replace("query All", "query Missing", StringComparison.Ordinal));
        _unknown = ReadExposure();
    }

    [Fact] void should_report_incomplete() => _unknown.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_unknown_not_unchanged() => _unknown.GetProperty("hasExposureChange").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_name_the_unavailable_side() => _unknown.GetProperty("afterAvailability").GetString().ShouldEqual("compilationFailed");
    [Fact] void should_not_guess_rows() => _unknown.GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
}
