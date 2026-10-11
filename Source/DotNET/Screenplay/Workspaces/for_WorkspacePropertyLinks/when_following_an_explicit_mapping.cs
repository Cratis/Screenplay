// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_following_an_explicit_mapping : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _source;
    WorkspacePropertyLink _target;

    void Establish() => _links = Create(Source);

    void Because()
    {
        _source = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();
        _target = _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Single(link => link.Reference.Role == "mapping-target");
    }

    [Fact] void should_record_the_source_role() => _source.Reference.Role.ShouldEqual("mapping-source");
    [Fact] void should_record_the_target_role() => _target.Reference.Role.ShouldEqual("mapping-target");
    [Fact] void should_connect_the_event_member() => _source.OtherEnd.Address.ShouldEqual("Projects.Registration.Register.Registered.name");
    [Fact] void should_connect_the_command_member() => _target.OtherEnd.Address.ShouldEqual("Projects.Registration.Register.Register.name");
    [Fact] void should_retain_the_mapping_owner() => _source.Reference.Owner.Address.ShouldEqual("Projects.Registration.Register.Register");
    [Fact] void should_retain_the_source_location() => _source.Reference.Location.Path.ShouldEqual("model.play");
}
