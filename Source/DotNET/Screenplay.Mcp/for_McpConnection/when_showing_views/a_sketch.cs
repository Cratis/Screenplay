// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class a_sketch : given.a_host_that_renders_views
{
    JsonElement _result;

    void Because() => _result = Call("visualize-model", new { sketch = new[] { new { path = "application.play", source = Source + RenamedSlice } } }).GetProperty("result");

    JsonElement Structured => _result.GetProperty("structuredContent");

    string Text => _result.GetProperty("content")[0].GetProperty("text").GetString()!;

    [Fact] void should_succeed() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_give_the_view_the_document_as_it_is() => Structured.GetProperty("documents")[0].GetProperty("source").GetString().ShouldEqual(Source);
    [Fact] void should_give_the_view_the_sketched_document() => Structured.GetProperty("changes")[0].GetProperty("source").GetString().ShouldEqual(Source + RenamedSlice);
    [Fact] void should_list_the_added_slice() => Structured.GetProperty("added").EnumerateArray().Select(added => added.GetString()).ShouldContain("Slice Projects.Registration.RenameProject");
    [Fact] void should_list_the_added_event() => Structured.GetProperty("added").EnumerateArray().Any(added => added.GetString()!.StartsWith("Event ", StringComparison.Ordinal) && added.GetString()!.EndsWith(".ProjectRenamed", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_remove_nothing() => Structured.GetProperty("removed").GetArrayLength().ShouldEqual(0);
    [Fact] void should_tell_the_model_what_is_added() => Text.Contains("Added: ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_write_the_sketch() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
