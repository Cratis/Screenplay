// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_a_described_command_maps_the_event_property : given.a_lineage_model
{
    JsonElement _report;
    JsonElement[] _hops = [];
    JsonElement _code;

    void Establish() => Write("""
        concept ProjectId : Uuid
        module Projects
          feature Portfolio
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name String
                handler
                  implementation
                    hint "Normalize the name before registering"
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
            slice StateView List
              readmodel ProjectList
                name String
              projection ProjectList
                from ProjectRegistered
        """);

    void Because()
    {
        _report = Trace("Projects.Portfolio.List.ProjectList.name");
        _hops = Hops(_report);
        _code = _hops.Single(hop => hop.GetProperty("role").GetString() == "code");
    }

    [Fact] void should_report_the_handler_as_opaque() => _code.GetProperty("status").GetString().ShouldEqual("opaque");
    [Fact] void should_reach_it_from_the_event_property() => Chain(_hops, _code).ShouldEqual(["read-model-property", "projection-mapping", "event-property", "code"]);
    [Fact] void should_name_the_command() => Text(_code, "address").ShouldEqual("Projects.Portfolio.Register.RegisterProject");
    [Fact] void should_not_follow_the_mapping_hint() => _hops.Any(hop => hop.GetProperty("role").GetString() is "production-mapping" or "command-property").ShouldBeFalse();
    [Fact] void should_not_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeFalse();
}
