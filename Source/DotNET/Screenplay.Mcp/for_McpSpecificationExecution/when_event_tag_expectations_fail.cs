// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_event_tag_expectations_fail : given.a_model
{
    JsonElement _report;
    string[] _failures;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        concept ThingId : Uuid
        module M
          feature F
            slice StateChange S
              command C
                thingId ThingId identifier
                produces E
                  for thingId
                  tag actual
                  thingId = thingId
              event E
                thingId ThingId
              specification X
                when C
                  thingId = "00000000-0000-0000-0000-000000000101"
                then E
                  tag expected
                  thingId = "00000000-0000-0000-0000-000000000101"
        """);

    void Because()
    {
        _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");
        _failures = _report.GetProperty("page").GetProperty("items")[0].GetProperty("failures").EnumerateArray().Select(failure => failure.GetString()!).ToArray();
    }

    [Fact] void should_fail_the_scenario() => _report.GetProperty("failed").GetInt32().ShouldEqual(1);
    [Fact] void should_show_the_expected_tags() => _failures.Any(failure => failure.Contains("Expected fact tags (null = unchecked): [[\"expected\"]]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_show_the_actual_tags() => _failures.Any(failure => failure.Contains("Actual fact tags: [[\"actual\"]]", StringComparison.Ordinal)).ShouldBeTrue();
}
