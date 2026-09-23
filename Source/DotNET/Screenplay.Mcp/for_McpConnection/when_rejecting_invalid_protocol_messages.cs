// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_rejecting_invalid_protocol_messages : given.a_connection
{
    JsonElement _beforeInitialization;
    JsonElement _unknownTool;
    JsonElement _invalidArguments;
    Exception? _oversized;

    void Because()
    {
        _beforeInitialization = Call("diagnostics");
        Initialize();
        _unknownTool = Call("not-a-tool");
        _invalidArguments = Call("find-declaration", new { name = 1 });
        using var input = new StringReader(new string('x', McpConnection.MaximumRequestCharacters + 1));
        using var output = new StringWriter();
        _oversized = Catch.Exception(() => Connection.Run(input, output));
    }

    [Fact] void should_require_initialization() => _beforeInitialization.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32600);
    [Fact] void should_reject_unknown_tools() => _unknownTool.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_non_string_arguments() => _invalidArguments.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_stop_an_oversized_stream() => _oversized.ShouldBeOfExactType<McpFailure>();
}
