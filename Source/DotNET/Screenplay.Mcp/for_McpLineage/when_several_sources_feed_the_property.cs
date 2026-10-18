// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_several_sources_feed_the_property : given.a_lineage_model
{
    JsonElement _report;
    JsonElement[] _hops = [];
    JsonElement[] _feeds = [];
    JsonElement[] _renamers = [];

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
            slice StateChange Rename
              command RenameProject
                projectId ProjectId identifier
                name String
                produces ProjectRenamed
                  for projectId
                  name = name
              command CorrectProjectName
                projectId ProjectId identifier
                correctedName String
                produces ProjectRenamed
                  for projectId
                  name = correctedName
              event ProjectRenamed
                name String
            slice StateView List
              readmodel ProjectList
                name String
              projection ProjectList
                from ProjectRegistered
                from ProjectRenamed
        """);

    void Because()
    {
        _report = Trace("Projects.Portfolio.List.ProjectList.name");
        _hops = Hops(_report);
        _feeds = Children(_hops, Subject(_hops));
        var renamed = _hops.Single(hop => Text(hop, "address") == "Projects.Portfolio.Rename.ProjectRenamed.name");
        _renamers = Children(_hops, renamed);
    }

    [Fact] void should_branch_once_per_feeding_event() => _feeds.Select(hop => hop.GetProperty("role").GetString()).ShouldEqual(["projection-mapping", "projection-mapping"]);
    [Fact] void should_follow_each_feeding_event() => _feeds.Select(feed => Text(Children(_hops, feed).Single(), "address")).ShouldEqual(["Projects.Portfolio.Register.ProjectRegistered.name", "Projects.Portfolio.Rename.ProjectRenamed.name"]);
    [Fact] void should_branch_once_per_producing_command() => _renamers.Select(hop => Text(hop, "address")).ShouldEqual(["Projects.Portfolio.Rename.CorrectProjectName", "Projects.Portfolio.Rename.RenameProject"]);
    [Fact] void should_follow_each_branch_to_its_own_input() => _renamers.Select(producer => Text(Children(_hops, producer).Single(), "address")).ShouldEqual(["Projects.Portfolio.Rename.CorrectProjectName.correctedName", "Projects.Portfolio.Rename.RenameProject.name"]);
    [Fact] void should_count_the_branches_on_the_hop() => Subject(_hops).GetProperty("branches").GetInt32().ShouldEqual(2);
    [Fact] void should_not_report_branches_as_breaks() => _report.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
