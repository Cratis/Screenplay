// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_automap_event_is_unresolved : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;

    void Establish() => _links = Create(Source.Replace("from Registered", "from Missing", StringComparison.Ordinal));
    void Because() => _complete = _links.Complete(_links.Find("Projects.Registration.Lookup.Project.name").Single());

    [Fact] void should_not_drop_the_unresolved_derived_link() => _complete.ShouldBeFalse();
}
