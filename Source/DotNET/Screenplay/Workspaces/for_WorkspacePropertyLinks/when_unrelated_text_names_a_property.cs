// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_unrelated_text_names_a_property : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;

    void Establish() => _links = Create(Source + "\n    slice StateView Unrelated\n      screen Other\n        title \"NAME\"\n");
    void Because() => _complete = _links.Complete(_links.Find("Projects.Registration.Lookup.Project.name").Single());

    [Fact] void should_keep_the_unrelated_property_complete() => _complete.ShouldBeTrue();
    [Fact] void should_not_create_unclassified_links() => _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Where(link => link.Reference.Role == "unclassified").ShouldBeEmpty();
}
