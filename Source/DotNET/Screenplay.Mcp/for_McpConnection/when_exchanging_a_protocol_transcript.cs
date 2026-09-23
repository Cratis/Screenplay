// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_exchanging_a_protocol_transcript : given.a_connection
{
    JsonElement[] _responses = [];

    void Because()
    {
        using var input = new StringReader("""
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"future-version","capabilities":{},"clientInfo":{"name":"spec","version":"1"}}}
            {"jsonrpc":"2.0","method":"notifications/initialized"}
            {"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":4}}
            {"jsonrpc":"2.0","id":"ping","method":"ping"}
            {"jsonrpc":"2.0","id":3,"method":"tools/list"}
            {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"merged-document","arguments":{}}}
            {"jsonrpc":"2.0","id":5,"method":"unknown"}
            not-json
            """);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        Connection.Run(input, output);
        _responses = [.. output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        })];
    }

    [Fact] void should_emit_only_six_response_lines() => _responses.Length.ShouldEqual(6);
    [Fact] void should_negotiate_the_supported_version() => _responses[0].GetProperty("result").GetProperty("protocolVersion").GetString().ShouldEqual("2025-06-18");
    [Fact] void should_preserve_string_request_ids() => _responses[1].GetProperty("id").GetString().ShouldEqual("ping");
    [Fact]
    void should_list_all_public_tools() => _responses[2].GetProperty("result").GetProperty("tools").EnumerateArray().Select(tool => tool.GetProperty("name").GetString()).ShouldContainOnly(
        ["describe-application", "find-declaration", "search-declarations", "declaration-details", "dependencies", "find-references",
        "find-fixtures", "find-assertion-gaps", "merged-document", "read-document", "diagnostics", "recommend-layout", "syntax-schema",
        "open-workspace", "workspace-state", "recover-workspace", "propose-rename", "read-workspace", "read-ast", "propose", "propose-ast",
        "expand-layout", "read-proposal", "export-workspace", "discard-proposal", "apply"]);
    [Fact] void should_return_unknown_method_error() => _responses[4].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32601);
    [Fact] void should_return_parse_error() => _responses[5].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32700);
    [Fact] void should_preserve_concrete_mapping_expression() => _responses[3].GetProperty("result").GetProperty("structuredContent").GetProperty("syntax").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("commands")[0].GetProperty("produces")[0].GetProperty("mappings")[1].GetProperty("source").GetProperty("path").GetString().ShouldEqual("name");
}
