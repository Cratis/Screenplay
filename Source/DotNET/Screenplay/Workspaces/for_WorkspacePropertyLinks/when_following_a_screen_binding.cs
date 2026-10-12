// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_following_a_screen_binding : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source);
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Single(link => link.Reference.Role == "screen-binding");

    [Fact] void should_retain_the_screen_owner() => _link.Reference.Owner.Address.ShouldEqual("Projects.Registration.Lookup.List");
    [Fact] void should_resolve_the_read_model_property() => _link.Resolution.ShouldEqual("resolved");
}
