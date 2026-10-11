// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_interaction_supplies_a_command_property : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _target;
    WorkspacePropertyLink _source;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Retry
              command Retry
                attemptId String
            slice StateView Lookup
              readmodel Project
                id String
              query Projects => Project[]
              screen List
                data Project[] via query Projects
                on click
                  execute Retry
                    with attemptId from item.id
        """);
    void Because()
    {
        _target = _links.Incoming(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).Single(link => link.Reference.Role == "mapping-target");
        _source = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.id").Single()).Single(link => link.Reference.Role == "mapping-source");
    }

    [Fact] void should_resolve_the_argument_as_a_mapping_target() => _target.Resolution.ShouldEqual("resolved");
    [Fact] void should_pin_the_target_to_the_argument_location() => _target.Reference.Location.Line.ShouldEqual(14);
    [Fact] void should_resolve_the_item_member_as_a_mapping_source() => _source.Resolution.ShouldEqual("resolved");
    [Fact] void should_connect_the_target_and_source() => _target.OtherEnd.ShouldEqual(_source.Candidates.Single());
    [Fact] void should_keep_the_command_property_complete() => _links.Complete(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).ShouldBeTrue();
    [Fact] void should_keep_the_read_model_property_complete() => _links.Complete(_links.Find("Projects.Registration.Lookup.Project.id").Single()).ShouldBeTrue();
}
