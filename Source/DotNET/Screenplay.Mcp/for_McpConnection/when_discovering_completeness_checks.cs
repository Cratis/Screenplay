// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_discovering_completeness_checks : given.a_connection
{
    JsonElement _schema;

    void Establish() => Initialize();
    void Because() => _schema = JsonSerializer.SerializeToElement(McpToolCatalog.Describe()).EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "diagnostics").GetProperty("inputSchema");

    [Fact] void should_expose_a_string_selection() => _schema.GetProperty("properties").GetProperty("checks").GetProperty("type").GetString().ShouldEqual("string");
}
