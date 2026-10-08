// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_counting_projection_feeds : given.a_model
{
    JsonElement _disabled;
    JsonElement _explicit;
    JsonElement _every;
    JsonElement _all;

    void Because()
    {
        _disabled = Report("      projection ProjectList\n        no automap\n        from ProjectRegistered\n        from ProjectUpdated");
        _explicit = Report("      projection ProjectList\n        no automap\n        from ProjectRegistered\n          name = name\n        from ProjectUpdated\n          name = name");
        _every = Report("      projection ProjectList\n        no automap\n        every\n          name = \"Known\"\n        from ProjectRegistered\n        from ProjectUpdated");
        _all = Report("      projection ProjectList\n        no automap\n        all\n          name = \"Known\"");
    }

    [Fact] void should_respect_no_automap() => FanIn(_disabled).ShouldBeEmpty();
    [Fact] void should_count_explicit_mappings() => FanIn(_explicit).Single().GetProperty("relatedCount").GetInt32().ShouldEqual(2);
    [Fact] void should_apply_every_to_subscribed_events() => FanIn(_every).Single().GetProperty("relatedCount").GetInt32().ShouldEqual(2);
    [Fact] void should_apply_all_to_known_events() => FanIn(_all).Single().GetProperty("relatedCount").GetInt32().ShouldEqual(3);

    JsonElement Report(string projection)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model.Replace("      projection ProjectList\n        from ProjectRegistered\n        from ProjectUpdated", projection, StringComparison.Ordinal));

        return Call("find-modeling-smells", new { propertyFanInThreshold = 1 }).GetProperty("result").GetProperty("structuredContent");
    }

    static IEnumerable<JsonElement> FanIn(JsonElement report) => report.GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL005");
}
