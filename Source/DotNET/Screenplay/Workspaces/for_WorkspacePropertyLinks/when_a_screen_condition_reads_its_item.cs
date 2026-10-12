// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_screen_condition_reads_its_item : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _condition;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateView Lookup
              readmodel Project
                status String
              query Projects => Project
              screen List
                data Project via query Projects
                on click
                  when item.status == "open"
                    notify info "Open"
        """);
    void Because() => _condition = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.status").Single()).Single(link => link.Reference.Role == "condition");

    [Fact] void should_classify_the_screen_reader() => _condition.Reference.Name.ShouldEqual("item.status");
    [Fact] void should_resolve_the_screen_subject_property() => _condition.Resolution.ShouldEqual("resolved");
    [Fact] void should_keep_the_property_complete() => _links.Complete(_links.Find("Projects.Registration.Lookup.Project.status").Single()).ShouldBeTrue();
}
