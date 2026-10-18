// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_a_command_produces_the_event_twice : given.a_lineage_model
{
    JsonElement[] _productions = [];

    void Establish() => Write("""
        concept ProjectId : Uuid
        module Projects
          feature Portfolio
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                budget Int
                produces ProjectRegistered
                  for projectId
                  budget = budget
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
                budget Int optional
            slice StateView List
              readmodel ProjectList
                budget Int optional
              projection ProjectList
                from ProjectRegistered
        """);

    void Because()
    {
        var hops = Hops(Trace("Projects.Portfolio.List.ProjectList.budget"));
        _productions = [.. hops.Where(hop => hop.GetProperty("role").GetString() == "production-mapping")];
    }

    [Fact] void should_report_both_productions() => _productions.Length.ShouldEqual(2);
    [Fact] void should_follow_the_mapping_production() => _productions.Count(hop => hop.GetProperty("status").GetString() == "resolved").ShouldEqual(1);
    [Fact] void should_report_the_other_production_as_missing() => _productions.Count(hop => hop.GetProperty("status").GetString() == "missing").ShouldEqual(1);
}
