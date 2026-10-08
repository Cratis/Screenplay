// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_checking_generic_names_and_shapes : given.a_model
{
    string[] _eventRules = [];
    string[] _commandRules = [];
    JsonElement _differentShape;
    JsonElement _sameProducer;
    JsonElement _repeatedProduction;

    void Because()
    {
        _eventRules = [.. new[] { "Updated", "Changed", "Edited", "Saved", "Modified", "Deleted", "Synced", "Received" }
            .Select(suffix => Report(Model.Replace("ProjectUpdated", "Project" + suffix, StringComparison.Ordinal)))
            .Select(report => Items(report).Single(item => item.GetProperty("ruleId").GetString() == "SMELL001").GetProperty("ruleId").GetString()!)];
        _commandRules = [.. new[] { "Update", "Edit", "Save", "Set", "Manage", "Get", "Load", "Fetch" }
            .Select(prefix => Report(Model.Replace("UpdateProject", prefix + "Project", StringComparison.Ordinal)))
            .Select(report => Items(report).Single(item => item.GetProperty("ruleId").GetString() == "SMELL002").GetProperty("ruleId").GetString()!)];
        _differentShape = Report(Model.Replace("event ProjectUpdated\n        name ProjectName", "event ProjectUpdated\n        name ProjectName optional", StringComparison.Ordinal));
        _sameProducer = Report(Model.Replace("        produces ProjectRegistered\n          for projectId\n          name = name", string.Empty, StringComparison.Ordinal)
            .Replace("        produces ProjectUpdated", "        produces ProjectRegistered\n          for projectId\n          name = name\n        produces ProjectUpdated", StringComparison.Ordinal));
        _repeatedProduction = Report(Model.Replace("        produces RevisionApproved\n          for projectId\n          approved = true", "        produces ProjectUpdated\n          for projectId\n          name = name", StringComparison.Ordinal));
    }

    [Fact] void should_cover_every_event_suffix() => _eventRules.Length.ShouldEqual(8);
    [Fact] void should_cover_every_command_prefix() => _commandRules.Length.ShouldEqual(8);
    [Fact] void should_distinguish_type_modifiers() => Items(_differentShape).Any(item => item.GetProperty("ruleId").GetString() == "SMELL003").ShouldBeFalse();
    [Fact] void should_not_flag_two_events_from_only_the_same_command() => Items(_sameProducer).Any(item => item.GetProperty("ruleId").GetString() == "SMELL003").ShouldBeFalse();
    [Fact] void should_count_distinct_produced_events_not_clauses() => Items(_repeatedProduction).Any(item => item.GetProperty("ruleId").GetString() == "SMELL004").ShouldBeFalse();

    JsonElement Report(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return Call("find-modeling-smells", new { eventFanOutThreshold = 1 }).GetProperty("result").GetProperty("structuredContent");
    }

    static IEnumerable<JsonElement> Items(JsonElement report) => report.GetProperty("page").GetProperty("items").EnumerateArray();
}
