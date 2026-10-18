// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_a_read_model_property_has_no_mapping : given.a_lineage_model
{
    JsonElement _report;
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
            slice StateView List
              readmodel ProjectList
                name String
                archivedAt String
              projection ProjectList
                from ProjectRegistered
        """);

    void Because()
    {
        _report = Trace("Projects.Portfolio.List.ProjectList.archivedAt");
        _hops = Hops(_report);
        _break = _hops.Single(hop => hop.GetProperty("status").GetString() == "missing");
    }

    [Fact] void should_place_the_break_directly_under_the_property() => _break.GetProperty("parent").GetInt32().ShouldEqual(Subject(_hops).GetProperty("id").GetInt32());
    [Fact] void should_name_the_absent_projection_mapping() => _break.GetProperty("role").GetString().ShouldEqual("projection-mapping");
    [Fact] void should_explain_the_break() => _break.GetProperty("reason").GetString()!.ShouldContain("archivedAt");
    [Fact] void should_not_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_list_the_break_in_the_summary() => _report.GetProperty("breaks").EnumerateArray().Single().GetProperty("id").GetInt32().ShouldEqual(_break.GetProperty("id").GetInt32());
}
