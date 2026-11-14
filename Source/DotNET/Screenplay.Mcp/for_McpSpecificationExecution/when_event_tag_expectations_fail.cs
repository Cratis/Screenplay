// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_event_tag_expectations_fail : given.a_model
{
    JsonElement _report;
    string[] _failures;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"),
        "module M\n  feature F\n    slice StateChange S\n      command C\n        produces E\n          tag actual\n      event E\n      specification X\n        when C\n        then E\n          tag expected\n");

    void Because()
    {
        _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");
        _failures = _report.GetProperty("page").GetProperty("items")[0].GetProperty("failures").EnumerateArray().Select(failure => failure.GetString()!).ToArray();
    }

    [Fact] void should_fail_the_scenario() => _report.GetProperty("failed").GetInt32().ShouldEqual(1);
    [Fact] void should_show_the_expected_tags() => _failures.Any(failure => failure.Contains("Expected fact tags (null = unchecked): [[\"expected\"]]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_show_the_actual_tags() => _failures.Any(failure => failure.Contains("Actual fact tags: [[\"actual\"]]", StringComparison.Ordinal)).ShouldBeTrue();
}
