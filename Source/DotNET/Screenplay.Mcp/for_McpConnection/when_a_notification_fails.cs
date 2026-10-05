// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_a_notification_fails : given.a_dynamic_connection
{
    readonly StringWriter _log = new();
    string? _response;
    JsonElement _opened;

    void Establish()
    {
        Connection.Log = _log;
        Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"roots":{"listChanged":true}},"clientInfo":{"name":"spec","version":"1"}}}""");
    }

    void Because()
    {
        // The client refuses roots/list, so handling notifications/initialized fails.
        const string refusal = /*lang=json,strict*/ """{"jsonrpc":"2.0","id":"screenplay-roots","error":{"code":-32601,"message":"Method not found"}}""";
        _response = Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/initialized"}""", new StringReader(refusal), new StringWriter());
        _opened = Call("open-workspace", new { path = ModelPath });
    }

    [Fact] void should_not_answer_the_notification() => _response.ShouldBeNull();
    [Fact] void should_report_the_failure_to_the_log() => _log.ToString().ShouldContain("The client refused roots/list");
    [Fact] void should_keep_the_session_usable() => Failed(_opened).ShouldBeFalse();
}
