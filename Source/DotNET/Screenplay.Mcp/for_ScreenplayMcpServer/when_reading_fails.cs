// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_reading_fails : for_McpConnection.given.a_connection
{
    readonly McpFailure _failure = new("Injected input failure.");
    TextReader _input = null!;
    Exception? _error;

    void Establish()
    {
        _input = Substitute.For<TextReader>();
        _input.Read().Throws(_failure);
    }

    void Because() => _error = Catch.Exception(() => ScreenplayMcpServer.Run(RootPath, _input, TextWriter.Null));

    [Fact] void should_propagate_the_original_transport_failure() => _error.ShouldEqual(_failure);
}
