// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_referencing_declaration_has_the_same_name_as_a_property : given.a_model
{
    WorkspacePropertyLinks _links;
    bool _complete;

    void Establish() => _links = Create(Source.Replace("command Register", "command name"));
    void Because() => _complete = _links.Complete(_links.Find("Projects.Registration.Register.Registered.name").Single());

    [Fact] void should_refuse_completeness_without_a_proven_field_or_parameter_declaration() => _complete.ShouldBeFalse();
    [Fact] void should_retain_the_name_as_unclassified_evidence() => _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Any(link => link.Reference.Role == "unclassified").ShouldBeTrue();
}
