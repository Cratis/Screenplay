// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_following_an_explicit_projection_mapping : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source.Replace("readmodel Project\n        name String", "readmodel Project\n        displayName String", StringComparison.Ordinal)
        .Replace("from Registered", "from Registered\n          displayName = name", StringComparison.Ordinal));
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.displayName").Single()).Single();

    [Fact] void should_record_the_target_role() => _link.Reference.Role.ShouldEqual("mapping-target");
    [Fact] void should_connect_the_event_source_property() => _link.OtherEnd.Address.ShouldEqual("Projects.Registration.Register.Registered.name");
}
