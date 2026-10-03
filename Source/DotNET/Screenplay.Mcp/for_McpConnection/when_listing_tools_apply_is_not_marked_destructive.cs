// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_listing_tools_apply_is_not_marked_destructive : given.a_connection
{
    JsonElement _tools = default;

    void Establish() => Initialize();

    void Because()
    {
        using var result = JsonDocument.Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}"""));
        _tools = result.RootElement.Clone().GetProperty("result").GetProperty("tools");
    }

    JsonElement Tool(string name) => _tools.EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == name);

    [Fact] void should_not_mark_apply_as_destructive() => Tool("apply").GetProperty("annotations").GetProperty("destructiveHint").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_mark_recovery_as_destructive() => Tool("recover-workspace").GetProperty("annotations").GetProperty("destructiveHint").GetBoolean().ShouldBeFalse();
    [Fact] void should_still_mark_apply_as_writing() => Tool("apply").GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean().ShouldBeFalse();
    [Fact] void should_mark_reads_as_read_only() => Tool("read-ast").GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean().ShouldBeTrue();
}
