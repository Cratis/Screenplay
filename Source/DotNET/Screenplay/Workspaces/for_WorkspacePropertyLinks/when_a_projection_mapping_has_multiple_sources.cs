// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_projection_mapping_has_multiple_sources : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _outgoing;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateView Lookup
              event Counted
                first String
                second String
              readmodel Total
                count String
              projection Totals => Total
                no automap
                from Counted
                  count = `${first}${second}`
        """);
    void Because() => _outgoing = [.. _links.Outgoing(_links.Find("Projects.Registration.Lookup.Counted.first").Single())];

    [Fact] void should_follow_the_source_to_the_target() => _outgoing.SelectMany(link => link.Candidates).Select(property => property.Address).Distinct().ShouldContainOnly("Projects.Registration.Lookup.Total.count");
    [Fact] void should_follow_the_target_to_both_sources() => _links.Outgoing(_links.Find("Projects.Registration.Lookup.Total.count").Single()).SelectMany(link => link.Candidates).Select(property => property.Name).Distinct().ShouldContainOnly("first", "second");
    [Fact] void should_retain_both_incoming_source_readers() => _links.Incoming(_links.Find("Projects.Registration.Lookup.Counted.second").Single()).Select(link => link.Reference.Role).ShouldContain("mapping-source");
}
