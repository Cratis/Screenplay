// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_writing_fails : for_McpConnection.given.a_connection
{
    readonly McpFailure _failure = new("Injected output failure.");
    TextWriter _output = null!;
    Exception? _error;

    void Establish()
    {
        _output = Substitute.For<TextWriter>();
        _output.When(writer => writer.WriteLine(Arg.Any<string>())).Throw(_failure);
    }

    void Because()
    {
        using var input = new StringReader("""{"jsonrpc":"2.0","id":1,"method":"ping"}""");
        _error = Catch.Exception(() => ScreenplayMcpServer.Run(RootPath, input, _output));
    }

    [Fact] void should_propagate_the_original_transport_failure() => _error.ShouldEqual(_failure);
}
