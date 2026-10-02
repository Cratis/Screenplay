// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class to_a_client_that_does_not_render_them : for_McpConnection.given.a_connection
{
    JsonElement _tools;
    JsonElement _resources;
    JsonElement _visualized;

    void Establish()
    {
        Connection = new(new McpTools(Root), new McpAppResources("<html></html>"));
        Initialize();
    }

    void Because()
    {
        _tools = Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""")!).GetProperty("result").GetProperty("tools");
        _resources = Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":2,"method":"resources/list"}""")!);
        _visualized = Call("visualize-model");
    }

    static JsonElement Parse(string response)
    {
        using var document = JsonDocument.Parse(response);
        return document.RootElement.Clone();
    }

    [Fact] void should_not_list_the_visualize_tool() => _tools.EnumerateArray().Any(tool => tool.GetProperty("name").GetString() == "visualize-model").ShouldBeFalse();
    [Fact] void should_not_offer_resources() => _resources.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32601);
    [Fact] void should_not_visualize() => _visualized.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
}
