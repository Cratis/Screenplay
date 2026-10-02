// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class the_model_on_disk : given.a_host_that_renders_views
{
    JsonElement _result;

    void Because() => _result = Call("visualize-model").GetProperty("result");

    JsonElement Structured => _result.GetProperty("structuredContent");

    string Text => _result.GetProperty("content")[0].GetProperty("text").GetString()!;

    [Fact] void should_succeed() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_give_the_view_the_document_path() => Structured.GetProperty("documents")[0].GetProperty("path").GetString().ShouldEqual("application.play");
    [Fact] void should_give_the_view_the_document_source() => Structured.GetProperty("documents")[0].GetProperty("source").GetString().ShouldEqual(Source);
    [Fact] void should_show_no_change() => Structured.GetProperty("changes").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_count_what_is_drawn() => Text.Contains("1 slice(s), 1 event(s), 0 error(s)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_source_from_the_model() => Text.Contains("RegisterProject", StringComparison.Ordinal).ShouldBeFalse();
}
