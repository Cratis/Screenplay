// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_rebinding_a_deleted_dynamic_root : given.a_connection
{
    string _deleted;
    string _replacement;
    JsonElement _opened;
    void Establish()
    {
        _deleted = Path.Combine(RootPath, "deleted");
        _replacement = Path.Combine(RootPath, "replacement");
        Directory.CreateDirectory(_deleted);
        Directory.CreateDirectory(_replacement);
        File.WriteAllText(Path.Combine(_replacement, "application.play"), Source);
        Connection = new(new McpTools());
        Initialize();
        Call("open-workspace", new { path = _deleted }).GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        Directory.Delete(_deleted);
    }
    void Because() => _opened = Call("open-workspace", new { path = _replacement }).GetProperty("result");
    [Fact] void should_rebind_without_inspecting_the_deleted_root() => _opened.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_open_the_new_models_documents() => _opened.GetProperty("structuredContent").GetProperty("documentCount").GetInt32().ShouldEqual(1);
}
