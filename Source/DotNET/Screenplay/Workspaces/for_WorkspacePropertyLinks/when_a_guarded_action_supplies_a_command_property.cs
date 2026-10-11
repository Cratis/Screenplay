// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_guarded_action_supplies_a_command_property : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _targets;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Retry
              command Retry
                attemptId String
            slice StateView Lookup
              readmodel Project
                id String
                status String
              query Projects => Project
              screen List
                data Project via query Projects
                action "Again"
                  when item.status == "failed" execute Retry
                    with attemptId from item.id
                  otherwise execute Retry
                    with attemptId from item.id
        """);
    void Because() => _targets = [.. _links.Incoming(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).Where(link => link.Reference.Role == "mapping-target")];

    [Fact] void should_classify_both_alternative_and_fallback_arguments() => _targets.Length.ShouldEqual(2);
    [Fact] void should_resolve_the_command_properties() => _targets.All(link => link.Resolution == "resolved").ShouldBeTrue();
    [Fact] void should_pin_the_links_to_the_argument_locations() => _targets.Select(link => link.Reference.Location.Line).ShouldContainOnly(15, 17);
    [Fact] void should_connect_both_arguments_to_the_item_member() => _targets.All(link => link.OtherEnd?.Address == "Projects.Registration.Lookup.Project.id").ShouldBeTrue();
    [Fact] void should_keep_the_command_property_complete() => _links.Complete(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).ShouldBeTrue();
}
