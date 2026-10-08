// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_an_identical_snapshot : given.two_snapshots
{
    JsonElement _result;

    void Because() => _result = Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = BeforeJson }).GetProperty("structuredContent");

    [Fact] void should_report_no_semantic_change() => _result.GetProperty("hasSemanticChange").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_a_complete_comparison() => _result.GetProperty("complete").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_an_empty_page() => _result.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
}
