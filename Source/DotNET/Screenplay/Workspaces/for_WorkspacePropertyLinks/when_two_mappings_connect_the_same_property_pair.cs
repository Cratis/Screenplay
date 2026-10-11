// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_two_mappings_connect_the_same_property_pair : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _sourceOutgoing;
    WorkspacePropertyLink[] _targetOutgoing;

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
                  count = first
                  count = `${first}${second}`
        """);

    void Because()
    {
        _sourceOutgoing = [.. _links.Outgoing(_links.Find("Projects.Registration.Lookup.Counted.first").Single())];
        _targetOutgoing = [.. _links.Outgoing(_links.Find("Projects.Registration.Lookup.Total.count").Single()).Where(link => link.Candidates.Any(property => property.Name == "first"))];
    }

    [Fact] void should_retain_both_outgoing_source_occurrences() => _sourceOutgoing.Length.ShouldEqual(2);
    [Fact] void should_retain_both_outgoing_target_occurrences() => _targetOutgoing.Length.ShouldEqual(2);
    [Fact] void should_retain_distinct_outgoing_source_locations() => _sourceOutgoing.Select(link => link.Reference.Location.Line).Distinct().Count().ShouldEqual(2);
    [Fact] void should_retain_distinct_outgoing_target_locations() => _targetOutgoing.Select(link => link.Reference.Location.Line).Distinct().Count().ShouldEqual(2);
    [Fact] void should_retain_both_incoming_source_occurrences() => _links.Incoming(_links.Find("Projects.Registration.Lookup.Counted.first").Single()).Count().ShouldEqual(2);
    [Fact] void should_retain_both_incoming_target_occurrences() => _links.Incoming(_links.Find("Projects.Registration.Lookup.Total.count").Single()).Count().ShouldEqual(2);
}
