// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_unrelated_declaration_has_the_same_member : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _incoming;

    void Establish() => _links = Create(Source + "\n    slice StateChange Unrelated\n      command Other\n        name String\n");
    void Because() => _incoming = [.. _links.Incoming(_links.Find("Projects.Registration.Unrelated.Other.name").Single())];

    [Fact] void should_not_link_by_global_property_name() => _incoming.ShouldBeEmpty();
}
