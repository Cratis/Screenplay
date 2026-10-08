// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_grouping_shapes_with_multiple_producers : given.a_model
{
    JsonElement[] _findings = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model
        .Replace("      event ProjectRegistered\n        name ProjectName", "      event ProjectRegistered\n        name ProjectName\n        approved Bool", StringComparison.Ordinal)
        .Replace("      event ProjectUpdated\n        name ProjectName", "      event ProjectUpdated\n        approved Bool\n        name ProjectName\n      event Unproduced\n        name ProjectName\n        approved Bool", StringComparison.Ordinal)
        .Replace("        produces ProjectUpdated", "        produces ProjectRegistered\n          for projectId\n          name = name\n        produces ProjectUpdated", StringComparison.Ordinal));

    void Because() => _findings = [.. Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL003")];

    [Fact] void should_group_the_same_property_set_independent_of_order() => _findings.Select(item => item.GetProperty("declaration").GetProperty("name").GetString()).ShouldContainOnly("ProjectRegistered", "ProjectUpdated");
    [Fact] void should_ignore_same_shaped_events_without_producers() => _findings.All(item => item.GetProperty("relatedCount").GetInt32() == 1).ShouldBeTrue();
}
