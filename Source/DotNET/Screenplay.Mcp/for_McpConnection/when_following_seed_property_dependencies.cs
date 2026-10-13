// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_following_seed_property_dependencies : given.a_connection
{
    JsonElement _fixture;

    void Establish()
    {
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\nseed\n  for \"name\"\n    ProjectRegistered\n      name = \"Ada\"\n");
        Initialize();
    }

    void Because()
    {
        var result = Call("dependencies", new { address = "Projects.Registration.RegisterProject.ProjectRegistered.name", kind = "Property", direction = "incoming" }).GetProperty("result").GetProperty("structuredContent");
        _fixture = result.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("reference").GetProperty("role").GetString() == "fixture");
    }

    [Fact] void should_resolve_the_seed_dependency() => _fixture.GetProperty("resolution").GetString().ShouldEqual("resolved");
    [Fact] void should_target_the_current_event_property() => _fixture.GetProperty("targets")[0].GetProperty("address").GetString().ShouldEqual("Projects.Registration.RegisterProject.ProjectRegistered.name");
    [Fact] void should_own_the_dependency_by_the_seed_entry() => _fixture.GetProperty("reference").GetProperty("owner").GetProperty("kind").GetString().ShouldEqual("SeedEvent");
}
