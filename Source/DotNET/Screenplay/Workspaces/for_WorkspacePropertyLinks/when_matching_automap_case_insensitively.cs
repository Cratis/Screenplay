// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_matching_automap_case_insensitively : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create(Source.Replace("readmodel Project\n        name String", "readmodel Project\n        nAME String", StringComparison.Ordinal));
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.nAME").Single()).Single(link => link.Reference.Role == "automap");

    [Fact] void should_use_the_projection_comparer() => _link.OtherEnd.Name.ShouldEqual("name");
}
