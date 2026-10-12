// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_indexing_seed_values : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _fixtures;

    void Establish() => _links = Create(Source + "\nseed\n  for \"name\"\n    Registered\n      name = \"Ada\"\n");
    void Because() => _fixtures = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Where(link => link.Reference.Role == "fixture")];

    [Fact] void should_link_the_seed_assignment() => _fixtures.Length.ShouldEqual(1);
    [Fact] void should_resolve_the_property() => _fixtures.Single().Resolution.ShouldEqual("resolved");
    [Fact] void should_own_the_assignment_by_the_seed_entry() => _fixtures.Single().Reference.Owner.Kind.ShouldEqual("SeedEvent");
    [Fact] void should_name_the_seed_event_owner() => _fixtures.Single().Reference.Owner.Name.ShouldEqual("Registered");
    [Fact] void should_locate_the_owner_at_the_event_entry() => _fixtures.Single().Reference.Owner.Location.Line.ShouldEqual(_fixtures.Single().Reference.Location.Line - 1);
    [Fact] void should_not_index_event_source_ids_as_properties() => _links.Links.Count(link => link.Reference.Owner?.Kind == "SeedEvent").ShouldEqual(1);
}
