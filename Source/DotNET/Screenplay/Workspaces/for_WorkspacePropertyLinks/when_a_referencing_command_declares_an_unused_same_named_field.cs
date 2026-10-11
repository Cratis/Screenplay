// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_referencing_command_declares_an_unused_same_named_field : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _incoming;

    void Establish() => _links = Create(Source.Replace("name = name", "name = \"constant\""));
    void Because() => _incoming = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single())];

    [Fact] void should_not_treat_the_unused_command_field_as_a_read() => _incoming.Any(link => link.Reference.Role == "unclassified").ShouldBeFalse();
    [Fact] void should_keep_the_event_property_complete() => _links.Complete(_links.Find("Projects.Registration.Register.Registered.name").Single()).ShouldBeTrue();
    [Fact] void should_not_link_the_unused_command_field_to_the_event_property() => _incoming.Any(link => link.OtherEnd?.Address == "Projects.Registration.Register.Register.name").ShouldBeFalse();
}
