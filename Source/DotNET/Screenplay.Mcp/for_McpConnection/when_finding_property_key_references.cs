// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_property_key_references : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateView Lookup
                  event Registered
                    sourceId Uuid
                  readmodel Project
                    name String
                  projection Projects => Project
                    no automap
                    from Registered key sourceId
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Projects.Registration.Lookup.Registered.sourceId", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_the_key_role() => _result.GetProperty("references")[0].GetProperty("role").GetString().ShouldEqual("key");
    [Fact] void should_report_the_projection_owner() => _result.GetProperty("references")[0].GetProperty("owner").GetProperty("address").GetString().ShouldEqual("Projects.Registration.Lookup.Projects");
    [Fact] void should_report_a_complete_link_set() => _result.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
