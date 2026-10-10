// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_a_missing_path : given.comparison_sources
{
    JsonElement _result;
    string _missing = null!;

    void Establish() => _missing = Path.Combine(RootPath, "missing");
    void Because() => _result = Compare(new { before = new { path = _missing }, after = new { path = _missing } });

    [Fact] void should_read_an_empty_model() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_no_changes() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    [Fact] void should_not_create_the_missing_root() => Directory.Exists(_missing).ShouldBeFalse();
}
