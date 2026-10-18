// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_tracing_explicit_automap_and_join_mappings : given.a_lineage_model
{
    const string View = "Projects.Portfolio.List.ProjectSummary";

    JsonElement _automap;
    JsonElement _explicit;
    JsonElement _join;
    JsonElement[] _joinHops = [];

    void Establish() => Write("""
        concept ProjectId : Uuid
        concept OwnerId : Uuid
        module Projects
          feature Portfolio
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                ownerId OwnerId
                name String
                produces ProjectRegistered
                  for projectId
                  ownerId = ownerId
                  name = name
              event ProjectRegistered
                ownerId OwnerId
                name String
            slice StateChange Enroll
              command RegisterOwner
                ownerId OwnerId identifier
                displayName String
                produces OwnerRegistered
                  for ownerId
                  displayName = displayName
              event OwnerRegistered
                displayName String
            slice StateView List
              readmodel ProjectSummary
                name String
                title String
                ownerId OwnerId
                ownerName String
              projection ProjectSummary
                from ProjectRegistered
                  title = name
                join owner on ownerId
                  with OwnerRegistered
                    ownerName = displayName
        """);

    void Because()
    {
        _automap = Mapping(Hops(Trace($"{View}.name")));
        _explicit = Mapping(Hops(Trace($"{View}.title")));
        _joinHops = Hops(Trace($"{View}.ownerName"));
        _join = Mapping(_joinHops);
    }

    [Fact] void should_report_an_automap_hop() => _automap.GetProperty("mode").GetString().ShouldEqual("automap");
    [Fact] void should_report_an_explicit_hop() => _explicit.GetProperty("mode").GetString().ShouldEqual("explicit");
    [Fact] void should_report_the_from_block() => _explicit.GetProperty("block").GetString().ShouldEqual("from");
    [Fact] void should_report_the_join_block() => _join.GetProperty("block").GetString().ShouldEqual("join");
    [Fact] void should_follow_the_join_to_the_joined_event() => _joinHops.Any(hop => Text(hop, "address") == "Projects.Portfolio.Enroll.OwnerRegistered.displayName").ShouldBeTrue();
    [Fact] void should_follow_the_join_to_the_command_input() => _joinHops.Any(hop => Text(hop, "address") == "Projects.Portfolio.Enroll.RegisterOwner.displayName").ShouldBeTrue();

    static JsonElement Mapping(JsonElement[] hops) => hops.Single(hop => hop.GetProperty("role").GetString() == "projection-mapping");
}
