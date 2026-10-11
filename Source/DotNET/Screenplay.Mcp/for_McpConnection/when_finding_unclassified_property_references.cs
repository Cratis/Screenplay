// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_unclassified_property_references : given.a_connection
{
    JsonElement _result;
    JsonElement _reference;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateView Lookup
                  readmodel Project
                    name String
                  query Projects => Project
                  screen List
                    data Project via query Projects
                    title "NAME"
            """);
        Initialize();
    }

    void Because()
    {
        _result = Call("find-references", new { address = "Projects.Registration.Lookup.Project.name", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");
        _reference = _result.GetProperty("incomplete")[0].GetProperty("reference");
    }

    [Fact] void should_disclose_the_unclassified_role() => _reference.GetProperty("role").GetString().ShouldEqual("unclassified");
    [Fact] void should_keep_the_property_incomplete() => _result.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_disclose_the_reason() => _reference.GetProperty("reasons").GetArrayLength().ShouldEqual(1);
}
