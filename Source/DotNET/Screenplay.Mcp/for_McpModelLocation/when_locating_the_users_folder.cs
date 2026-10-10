// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_the_users_folder : given_a_project
{
    string _located = null!;

    void Because() => _located = McpModelLocation.User(Project);

    [Fact] void should_use_screenplay_inside_documents() => _located.ShouldEqual(PathOf("Screenplay"));
    [Fact] void should_not_create_it() => Directory.Exists(_located).ShouldBeFalse();
}
