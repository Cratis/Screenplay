// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_reading_a_missing_root : for_McpConnection.given.a_connection
{
    string _missingRoot = null!;
    JsonElement[] _responses = [];

    void Establish() => _missingRoot = Path.Combine(RootPath, "Screenplay");

    void Because()
    {
        using var input = new StringReader("""
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"embedding-spec","version":"1"}}}
            {"jsonrpc":"2.0","method":"notifications/initialized"}
            {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"open-workspace","arguments":{}}}
            {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"describe-application","arguments":{}}}
            {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"diagnostics","arguments":{}}}
            {"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"workspace-state","arguments":{}}}
            """);
        using var output = new StringWriter();
        ScreenplayMcpServer.Run(_missingRoot, input, output);
        _responses = [.. output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        })];
    }

    [Fact] void should_answer_every_request() => _responses.Length.ShouldEqual(5);
    [Fact] void should_accept_every_read() => _responses.Skip(1).All(response => !response.GetProperty("result").GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_open_an_empty_workspace() => _responses[1].GetProperty("result").GetProperty("structuredContent").GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_absent_state() => _responses[4].GetProperty("result").GetProperty("structuredContent").GetProperty("exists").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_create_the_root_or_metadata() => Directory.Exists(_missingRoot).ShouldBeFalse();
}
