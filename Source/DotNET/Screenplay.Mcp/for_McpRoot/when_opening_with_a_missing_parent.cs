// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_opening_with_a_missing_parent : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Root = new(Path.Combine(RootPath, "missing", "Screenplay")));

    [Fact] void should_reject_the_missing_parent() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_create_the_parent() => Directory.Exists(Path.Combine(RootPath, "missing")).ShouldBeFalse();
}
