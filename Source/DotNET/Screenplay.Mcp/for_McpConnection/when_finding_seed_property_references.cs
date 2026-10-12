// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_seed_property_references : given.a_connection
{
    JsonElement _result;
    JsonElement[] _fixtures;

    void Establish()
    {
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\nseed\n  for \"name\"\n    ProjectRegistered\n      name = \"Ada\"\n");
        Initialize();
    }

    void Because()
    {
        _result = Call("find-references", new { address = "Projects.Registration.RegisterProject.ProjectRegistered.name", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");
        _fixtures = [.. _result.GetProperty("references").EnumerateArray().Where(reference => reference.GetProperty("role").GetString() == "fixture")];
    }

    [Fact] void should_report_the_seed_fixture() => _fixtures.Length.ShouldEqual(1);
    [Fact] void should_own_the_fixture_by_the_seed_entry() => _fixtures.Single().GetProperty("owner").GetProperty("kind").GetString().ShouldEqual("SeedEvent");
    [Fact] void should_name_the_seed_owner() => _fixtures.Single().GetProperty("owner").GetProperty("name").GetString().ShouldEqual("ProjectRegistered");
    [Fact] void should_report_complete_references() => _result.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
