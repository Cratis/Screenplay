// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_automap_is_disabled : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _mapped;

    void Establish() => _links = Create(Source.Replace("projection Projects => Project", "projection Projects => Project\n        no automap", StringComparison.Ordinal));
    void Because() => _mapped = [.. _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Where(link => link.Reference.Role == "automap")];

    [Fact] void should_not_invent_a_derived_link() => _mapped.ShouldBeEmpty();
}
