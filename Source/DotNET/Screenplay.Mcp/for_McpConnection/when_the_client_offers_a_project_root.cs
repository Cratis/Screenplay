// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_offers_a_project_root : given.a_dynamic_connection
{
    string _project = null!;

    void Establish()
    {
        _project = Path.Combine(EmptyPath, "project");
        Directory.CreateDirectory(Path.Combine(_project, "Source"));
        Initialize(true, RootsAnswer(_project));
    }

    void Because() => Call("open-workspace");

    [Fact] void should_remember_the_root_the_host_offered() => Tools.ClientDerivedRootPath.ShouldEqual(_project);
    [Fact] void should_not_create_a_model_folder_beside_the_source() => Directory.Exists(Path.Combine(_project, "Screenplay")).ShouldBeFalse();
}
