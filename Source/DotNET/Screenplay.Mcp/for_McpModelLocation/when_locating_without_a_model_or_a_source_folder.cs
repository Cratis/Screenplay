// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_without_a_model_or_a_source_folder : given_a_project
{
    string _located = null!;

    void Because() => _located = McpModelLocation.Project(Project);

    [Fact] void should_fall_back_to_a_screenplay_folder() => _located.ShouldEqual(PathOf("Screenplay"));
    [Fact] void should_not_create_it() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
    [Fact] void should_not_touch_the_configuration_folder() => Directory.Exists(PathOf(".cratis")).ShouldBeFalse();
}
