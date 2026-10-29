// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_running_without_a_root : Specification
{
    Exception? _error;

    // A literal null root must keep binding to the dynamic-root overload; another three-argument overload makes this call ambiguous.
    void Because() => _error = Catch.Exception(() => ScreenplayMcpServer.Run(null, TextReader.Null, TextWriter.Null));

    [Fact] void should_start_a_dynamic_server_and_end_with_the_input() => _error.ShouldBeNull();
}
