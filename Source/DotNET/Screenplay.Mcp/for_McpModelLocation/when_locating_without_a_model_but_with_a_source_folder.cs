// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_without_a_model_but_with_a_source_folder : given_a_project
{
    string _located = null!;

    void Establish() => Directory.CreateDirectory(PathOf("src"));

    void Because() => _located = McpModelLocation.Project(Project);

    [Fact] void should_build_from_the_source_folder() => _located.ShouldEqual(PathOf("src"));
}
