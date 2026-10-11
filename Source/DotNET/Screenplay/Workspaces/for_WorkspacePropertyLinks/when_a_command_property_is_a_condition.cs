// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_command_property_is_a_condition : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source.Replace("produces Registered\n          name = name", "produces when name == \"Ready\"\n          Registered\n            name = name", StringComparison.Ordinal));
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single(link => link.Reference.Role == "condition");

    [Fact] void should_resolve_the_command_member() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_retain_the_condition_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}
