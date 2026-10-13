// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_seed_values_use_the_current_generation : given.a_model
{
    WorkspacePropertyLinks _links;

    void Because() => _links = Create(Source.Replace("event Registered\n        name String", "event Registered generation 1\n        name String\n      event Registered generation 2\n        name String", StringComparison.Ordinal) + "\nseed\n  for \"project-1\"\n    Registered\n      name = \"Ada\"\n");

    [Fact] void should_link_only_the_current_seed_property() => _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Count(link => link.Reference.Role == "fixture").ShouldEqual(1);
    [Fact] void should_leave_historical_properties_unreferenced_by_seeds() => _links.Incoming(_links.Find("Projects.Registration.Register.Registered.generation1.name").Single()).ShouldBeEmpty();
}
