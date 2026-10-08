// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_counting_effective_projection_feeds : given.a_model
{
    JsonElement _aggregate;
    JsonElement _entry;
    JsonElement _join;

    void Because()
    {
        _aggregate = Report(Model.Replace("      readmodel ProjectList\n        name ProjectName", "      readmodel ProjectList\n        name ProjectName\n        count Int", StringComparison.Ordinal)
            .Replace("        from ProjectRegistered\n        from ProjectUpdated", "        from ProjectRegistered\n          increment count\n        from ProjectUpdated\n          increment count", StringComparison.Ordinal));
        _entry = Report(Model.Replace("        name ProjectName\n      projection ProjectList", "        name ProjectName\n        id ProjectId identifier\n      projection ProjectList", StringComparison.Ordinal)
            .Replace("        from ProjectRegistered\n        from ProjectUpdated", "        variant ProjectList\n          enters on ProjectRegistered\n          enters on ProjectUpdated", StringComparison.Ordinal));
        _join = Report(Model.Replace("      readmodel ProjectList\n        name ProjectName", "      readmodel ProjectList\n        name ProjectName\n        label ProjectName", StringComparison.Ordinal)
            .Replace("        from ProjectRegistered\n        from ProjectUpdated", "        join label on name\n          with ProjectRegistered\n            label = name\n          with ProjectUpdated\n            label = name", StringComparison.Ordinal));
    }

    [Fact] void should_not_automap_unrelated_properties_from_aggregate_only_blocks() => Feeds(_aggregate).Any(item => item.GetProperty("property").GetString() == "name").ShouldBeFalse();
    [Fact] void should_keep_the_explicit_aggregate_feeds() => Feeds(_aggregate).Single().GetProperty("property").GetString().ShouldEqual("count");
    [Fact] void should_count_variant_entry_events_without_authored_from_blocks() => Feeds(_entry).Single().GetProperty("relatedCount").GetInt32().ShouldEqual(2);
    [Fact] void should_exclude_explicit_join_sources_from_automap() => Feeds(_join).Single().GetProperty("property").GetString().ShouldEqual("label");

    JsonElement Report(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return Call("find-modeling-smells", new { propertyFanInThreshold = 1 }).GetProperty("result").GetProperty("structuredContent");
    }

    static IEnumerable<JsonElement> Feeds(JsonElement report) => report.GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL005");
}
