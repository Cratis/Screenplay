// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_counting_feeds_through_all_blocks : given.a_model
{
    JsonElement[] _payload = [];
    JsonElement[] _literal = [];
    JsonElement[] _autoMap = [];

    void Because()
    {
        _payload = FanIn(Report("      projection ProjectList\n        no automap\n        all\n          name = name"));
        _literal = FanIn(Report("      projection ProjectList\n        no automap\n        all\n          name = \"Known\""));
        _autoMap = FanIn(Report("      projection ProjectList\n        all"));
    }

    [Fact] void should_count_only_events_carrying_the_mapped_value() => _payload.Single().GetProperty("relatedCount").GetInt32().ShouldEqual(2);
    [Fact] void should_not_relate_events_that_do_not_carry_the_mapped_value() => _payload.Single().GetProperty("related").EnumerateArray().Select(related => related.GetProperty("name").GetString()).ShouldContainOnly("ProjectRegistered", "ProjectUpdated");
    [Fact] void should_not_count_every_event_for_a_value_no_event_supplies() => _literal.ShouldBeEmpty();
    [Fact] void should_count_only_automapped_events() => _autoMap.Single().GetProperty("relatedCount").GetInt32().ShouldEqual(2);

    JsonElement Report(string projection)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model.Replace("      projection ProjectList\n        from ProjectRegistered\n        from ProjectUpdated", projection, StringComparison.Ordinal));

        return Call("find-modeling-smells", new { propertyFanInThreshold = 1 }).GetProperty("result").GetProperty("structuredContent");
    }

    static JsonElement[] FanIn(JsonElement report) => [.. report.GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL005")];
}
