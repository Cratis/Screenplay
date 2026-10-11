// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_selecting_the_current_event_generation : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;
    WorkspacePropertyLink[] _historical;

    void Establish() => _links = Create(Source.Replace("event Registered\n        name String", "event Registered generation 1\n        name String\n      event Registered generation 2\n        name String", StringComparison.Ordinal));
    void Because()
    {
        _link = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();
        _historical = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Registered.generation1.name").Single())];
    }

    [Fact] void should_connect_the_current_property() => _link.OtherEnd.Address.ShouldEqual("Projects.Registration.Register.Registered.name");
    [Fact] void should_not_connect_a_historical_shape_by_name() => _historical.ShouldBeEmpty();
    [Fact] void should_label_the_historical_target() => _links.Find("Projects.Registration.Register.Registered.generation1.name").Single().Details.ShouldNotBeNull();
}
