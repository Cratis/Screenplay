// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_interaction_target_is_unresolved : given.a_model
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
              query Projects => Project
              screen List
                data Project via query Projects
                on click
                  execute Missing
                    with attemptId from item.id
        """);
    void Because()
    {
        _target = _links.Incoming(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).Single(link => link.Reference.Role == "mapping-target");
        _source = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.id").Single()).Single(link => link.Reference.Role == "mapping-source");
    }

    [Fact] void should_retain_the_unresolved_target_evidence() => _target.Resolution.ShouldEqual("incomplete");
    [Fact] void should_keep_the_source_incomplete() => _source.Resolution.ShouldEqual("incomplete");
    [Fact] void should_not_drop_the_command_property_reader() => _links.Complete(_links.Find("Projects.Registration.Retry.Retry.attemptId").Single()).ShouldBeFalse();
    [Fact] void should_not_claim_the_read_model_property_is_complete() => _links.Complete(_links.Find("Projects.Registration.Lookup.Project.id").Single()).ShouldBeFalse();
}
