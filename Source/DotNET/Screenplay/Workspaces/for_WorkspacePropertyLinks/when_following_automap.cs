// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_following_automap : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source);
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Single(link => link.Reference.Role == "automap");

    [Fact] void should_connect_the_event_property() => _link.OtherEnd.Address.ShouldEqual("Projects.Registration.Register.Registered.name");
    [Fact] void should_resolve_the_derived_link() => _link.Resolution.ShouldEqual("resolved");
}
