// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_referencing_command_declares_a_same_named_read_alias : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;

    void Establish() => _links = Create(Source
        .Replace("name String", "label String")
        .Replace("name = name", "label = label")
        .Replace("readmodel Project\n        label String", "readmodel Project\n        name String")
        .Replace("produces Registered", "reads Project as name\n        produces Registered"));
    void Because() => _complete = _links.Complete(_links.Find("Projects.Registration.Lookup.Project.name").Single());

    [Fact] void should_not_treat_the_read_alias_as_a_property_reader() => _complete.ShouldBeTrue();
    [Fact] void should_not_create_an_unclassified_link_for_the_alias() => _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Any(link => link.Reference.Role == "unclassified").ShouldBeFalse();
}
