// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_a_source_folder_holding_workspace_state : given_a_project
{
    string _located = null!;

    void Establish() => Directory.CreateDirectory(PathOf("src/.screenplay"));

    void Because() => _located = McpModelLocation.Project(Project);

    [Fact] void should_keep_serving_the_source_folder() => _located.ShouldEqual(PathOf("src"));
}
