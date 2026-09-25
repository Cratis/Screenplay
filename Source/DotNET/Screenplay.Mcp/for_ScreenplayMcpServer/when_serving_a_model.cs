// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_serving_a_model : for_McpConnection.given.a_connection
{
    StringReader _input = null!;
    StringWriter _output = null!;
    JsonElement[] _responses = [];

    void Establish()
    {
        _input = new StringReader("""
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"embedding-spec","version":"1"}}}
            {"jsonrpc":"2.0","method":"notifications/initialized"}
            {"jsonrpc":"2.0","id":2,"method":"tools/list"}
            {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"open-workspace","arguments":{}}}
            {"jsonrpc":"2.0","id":4,"method":"unknown"}
            """);
        _output = new StringWriter(CultureInfo.InvariantCulture);
    }

    void Because()
    {
        ScreenplayMcpServer.Run(RootPath, _input, _output);
        _responses = [.. _output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        })];
    }

    [Fact] void should_emit_only_four_protocol_responses() => _responses.Length.ShouldEqual(4);
    [Fact] void should_initialize_the_screenplay_server() => _responses[0].GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString().ShouldEqual("cratis.screenplay");
    [Fact] void should_discover_all_tools() => _responses[1].GetProperty("result").GetProperty("tools").GetArrayLength().ShouldEqual(27);
    [Fact] void should_open_the_model_from_the_supplied_root() => _responses[2].GetProperty("result").GetProperty("structuredContent").GetProperty("documentCount").GetInt32().ShouldEqual(1);
    [Fact] void should_return_request_failures_as_protocol_errors() => _responses[3].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32601);
    [Fact] void should_return_at_eof_without_closing_input() => _input.Read().ShouldEqual(-1);
    [Fact] void should_leave_output_owned_by_the_caller() => Catch.Exception(() => _output.Write("caller still owns output")).ShouldBeNull();
    [Fact] void should_preserve_source_without_an_apply_request() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_not_create_identity_state_during_reading() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();

    void Destroy()
    {
        _input.Dispose();
        _output.Dispose();
    }
}
