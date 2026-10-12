// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_indexing_fan_out_fields : given.a_model
{
    WorkspacePropertyLinks _links;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
                produces Registered
                  name = name
                produces Audited
                  name = name
              event Registered
                name String
              event Audited
                name String
        """);

    void Because() => _links.Find("Projects.Registration.Register.Register.name").ShouldNotBeEmpty();

    [Fact] void should_keep_the_command_link_set_complete() => _links.Complete(_links.Find("Projects.Registration.Register.Register.name").Single()).ShouldBeTrue();
    [Fact] void should_keep_each_event_link_set_complete() => new[] { "Registered", "Audited" }.All(owner => _links.Complete(_links.Find($"Projects.Registration.Register.{owner}.name").Single())).ShouldBeTrue();
    [Fact] void should_retain_each_distinct_mapping_occurrence() => _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Count(link => link.Reference.Role == "mapping-source").ShouldEqual(2);
}
