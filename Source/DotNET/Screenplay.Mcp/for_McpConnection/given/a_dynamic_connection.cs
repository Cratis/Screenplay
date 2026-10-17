// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

public class a_dynamic_connection : Specification
{
    string _parent = null!;
    internal string ModelPath = null!;
    internal string EmptyPath = null!;
    internal string DocumentsPath = null!;
    internal McpTools Tools = null!;
    internal McpConnection Connection = null!;

    void Establish()
    {
        _parent = SpecFixtureDirectory.Create("mcp-dynamic-connection");
        ModelPath = Path.Combine(_parent, "model");
        EmptyPath = Path.Combine(_parent, "empty");
        Directory.CreateDirectory(ModelPath);
        Directory.CreateDirectory(EmptyPath);
        File.WriteAllText(Path.Combine(ModelPath, "application.play"), a_connection.Source, new UTF8Encoding(false));
        DocumentsPath = Path.Combine(_parent, "documents");
        Directory.CreateDirectory(DocumentsPath);
        Tools = new McpTools { CurrentDirectoryHint = EmptyPath, DocumentsDirectoryHint = DocumentsPath };
        Connection = new(Tools);
    }

    internal static string RootsAnswer(params string[] paths) =>
        JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = McpConnection.RootsRequestId,
            result = new { roots = paths.Select(path => new { uri = new Uri(path).AbsoluteUri, name = Path.GetFileName(path) }) }
        });

    internal void Initialize(bool roots, string? answer = null)
    {
        var capabilities = roots ? """{"roots":{"listChanged":true}}""" : "{}";
        Connection.Handle(
            "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":" +
            capabilities +
            ",\"clientInfo\":{\"name\":\"spec\",\"version\":\"1\"}}}");
        Connection.Handle(
            /*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            new StringReader(answer ?? string.Empty),
            new StringWriter());
    }

    internal JsonElement Call(string name, object? arguments = null)
    {
        var request = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "tools/call", @params = new { name, arguments = arguments ?? new { } } });
        using var result = JsonDocument.Parse(Connection.Handle(request));
        return result.RootElement.Clone();
    }

    internal static bool Failed(JsonElement response) => response.GetProperty("result").GetProperty("isError").GetBoolean();

    internal static string Text(JsonElement response) => response.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;

    void Destroy() => SpecFixtureDirectory.Delete(_parent);
}
