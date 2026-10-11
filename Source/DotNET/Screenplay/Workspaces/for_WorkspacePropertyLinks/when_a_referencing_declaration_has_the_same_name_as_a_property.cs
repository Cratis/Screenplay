// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_referencing_declaration_has_the_same_name_as_a_property : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;

    void Establish() => _links = Create(Source.Replace("command Register", "command name"));
    void Because() => _complete = _links.Complete(_links.Find("Projects.Registration.Register.Registered.name").Single());

    [Fact] void should_not_treat_the_command_name_as_a_property_reader() => _complete.ShouldBeTrue();
    [Fact] void should_not_create_an_unclassified_link_for_the_declaration_name() => _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Any(link => link.Reference.Role == "unclassified").ShouldBeFalse();
}
