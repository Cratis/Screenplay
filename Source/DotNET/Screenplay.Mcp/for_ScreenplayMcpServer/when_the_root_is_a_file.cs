// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_the_root_is_a_file : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ScreenplayMcpServer.Run(Path.Combine(RootPath, "application.play"), TextReader.Null, TextWriter.Null));

    [Fact] void should_propagate_the_startup_failure() => _error.ShouldBeOfExactType<McpFailure>();
}
