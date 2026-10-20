// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_without_a_model_but_with_a_code_folder : given_a_project
{
    string _located = null!;

    void Establish()
    {
        Directory.CreateDirectory(PathOf("src/main/java"));
        File.WriteAllText(PathOf("src/main/java/Owner.java"), "class Owner {}\n");
    }

    void Because() => _located = McpModelLocation.Project(Project);

    [Fact] void should_fall_back_to_a_screenplay_folder() => _located.ShouldEqual(PathOf("Screenplay"));
    [Fact] void should_not_create_it() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
}
