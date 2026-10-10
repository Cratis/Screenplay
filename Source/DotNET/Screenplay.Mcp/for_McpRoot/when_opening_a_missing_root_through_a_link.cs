// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_opening_a_missing_root_through_a_link : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Establish() => Directory.CreateSymbolicLink(Path.Combine(RootPath, "linked"), RootPath);

    void Because() => _error = Catch.Exception(() => Root = new(Path.Combine(RootPath, "linked", "Screenplay")));

    [Fact] void should_reject_the_linked_ancestor() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_create_the_root_at_the_target() => Directory.Exists(Path.Combine(RootPath, "Screenplay")).ShouldBeFalse();
}
