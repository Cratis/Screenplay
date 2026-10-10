// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_opening_a_file_as_the_root : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Root = new(Path.Combine(RootPath, "application.play")));

    [Fact] void should_reject_the_file() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_preserve_the_file() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
