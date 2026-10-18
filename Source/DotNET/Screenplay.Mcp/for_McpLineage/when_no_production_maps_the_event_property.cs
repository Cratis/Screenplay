// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_no_production_maps_the_event_property : given.a_lineage_model
{
    JsonElement[] _hops = [];
    JsonElement _break;

    void Establish() => Write("""
        concept ProjectId : Uuid
        module Projects
          feature Portfolio
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
                budget Int optional
            slice StateView List
              readmodel ProjectList
                budget Int optional
              projection ProjectList
                from ProjectRegistered
        """);

    void Because()
    {
        _hops = Hops(Trace("Projects.Portfolio.List.ProjectList.budget"));
        _break = _hops.Single(hop => hop.GetProperty("status").GetString() == "missing");
    }

    [Fact] void should_reach_the_event_property_through_automap() => Chain(_hops, _break).ShouldEqual(["read-model-property", "projection-mapping", "event-property", "production-mapping"]);
    [Fact] void should_name_the_producer_that_does_not_map_it() => Text(_break, "address").ShouldEqual("Projects.Portfolio.Register.RegisterProject");
    [Fact] void should_explain_the_break() => _break.GetProperty("reason").GetString()!.ShouldContain("budget");
}
