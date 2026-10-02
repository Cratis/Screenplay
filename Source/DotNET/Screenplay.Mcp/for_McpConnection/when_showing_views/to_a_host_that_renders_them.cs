// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class to_a_host_that_renders_them : given.a_host_that_renders_views
{
    JsonElement _tools;
    JsonElement _resources;
    JsonElement _board;

    void Because()
    {
        _tools = Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""").GetProperty("result").GetProperty("tools");
        _resources = Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":2,"method":"resources/list"}""").GetProperty("result").GetProperty("resources");
        _board = Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"ui://screenplay/event-model-board.html"}}""").GetProperty("result").GetProperty("contents")[0];
    }

    JsonElement Visualize => _tools.EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "visualize-model");

    [Fact] void should_offer_resources() => Initialized.GetProperty("result").GetProperty("capabilities").TryGetProperty("resources", out _).ShouldBeTrue();
    [Fact] void should_acknowledge_the_extension() => Initialized.GetProperty("result").GetProperty("capabilities").GetProperty("extensions").TryGetProperty("io.modelcontextprotocol/ui", out _).ShouldBeTrue();
    [Fact] void should_tell_the_model_about_the_board() => Initialized.GetProperty("result").GetProperty("instructions").GetString()!.Contains("visualize-model", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_visualize_tool() => Visualize.GetProperty("name").GetString().ShouldEqual("visualize-model");
    [Fact] void should_link_the_tool_to_the_board() => Visualize.GetProperty("_meta").GetProperty("ui").GetProperty("resourceUri").GetString().ShouldEqual("ui://screenplay/event-model-board.html");
    [Fact] void should_mark_the_tool_read_only() => Visualize.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean().ShouldBeTrue();
    [Fact] void should_list_the_board() => _resources[0].GetProperty("uri").GetString().ShouldEqual("ui://screenplay/event-model-board.html");
    [Fact] void should_list_the_board_as_an_app() => _resources[0].GetProperty("mimeType").GetString().ShouldEqual("text/html;profile=mcp-app");
    [Fact] void should_read_the_board_page() => _board.GetProperty("text").GetString().ShouldEqual(BoardHtml);
    [Fact] void should_read_the_board_as_an_app() => _board.GetProperty("mimeType").GetString().ShouldEqual("text/html;profile=mcp-app");
    [Fact] void should_allow_the_icon_font_origin() => _board.GetProperty("_meta").GetProperty("ui").GetProperty("csp").GetProperty("resourceDomains")[0].GetString().ShouldEqual("https://cdn.jsdelivr.net");
}
