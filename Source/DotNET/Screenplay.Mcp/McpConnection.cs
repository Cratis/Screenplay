// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

sealed class McpConnection(McpTools tools)
{
    internal const int MaximumRequestCharacters = 32 * 1024 * 1024;
    bool _initialized;
    bool _ready;

    internal void Run(TextReader input, TextWriter output)
    {
        while (ReadLine(input) is { } line)
        {
            var response = Handle(line);
            if (response is not null)
            {
                output.WriteLine(response);
                output.Flush();
            }
        }
    }

    internal string? Handle(string line)
    {
        object? id = null;
        try
        {
            using var document = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 256 });
            var request = document.RootElement;
            if (request.ValueKind != JsonValueKind.Object ||
                !request.TryGetProperty("jsonrpc", out var version) || version.ValueKind != JsonValueKind.String || version.GetString() != "2.0" ||
                !request.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String)
            {
                throw new McpFailure("Expected one JSON-RPC 2.0 request object.", -32600);
            }

            var members = request.EnumerateObject().Select(property => property.Name).ToArray();
            if (members.Distinct(StringComparer.Ordinal).Count() != members.Length)
            {
                throw new McpFailure("Duplicate JSON-RPC members are not admitted.", -32600);
            }

            var hasId = request.TryGetProperty("id", out var requestId);
            if (hasId)
            {
                if (requestId.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
                {
                    throw new McpFailure("Request id must be a string or number.", -32600);
                }

                id = requestId.Clone();
            }

            var name = method.GetString()!;
            if (!hasId)
            {
                if (name == "notifications/initialized" && _initialized)
                {
                    _ready = true;
                }

                // Notifications never receive responses, including unsupported notifications.
                return null;
            }

            var parameters = request.TryGetProperty("params", out var supplied) ? supplied : McpJson.Empty;
            if (parameters.ValueKind != JsonValueKind.Object)
            {
                throw new McpFailure("Parameters must be an object.", -32602);
            }

            var result = Dispatch(name, parameters);
            return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, McpJson.Options);
        }
        catch (JsonException)
        {
            return Error(null, -32700, "Invalid JSON.");
        }
        catch (McpFailure failure)
        {
            return Error(id, failure.Code == 0 ? -32603 : failure.Code, failure.Message);
        }
        catch (Exception exception)
        {
            // Protocol errors remain protocol messages; a failed request never terminates as a success.
            return Error(id, -32603, $"Request failed: {exception.Message}");
        }
    }

    static string Error(object? id, int code, string message) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } }, McpJson.Options);

    static string? ReadLine(TextReader input)
    {
        var text = new StringBuilder();
        while (input.Read() is var character && character >= 0)
        {
            if (character == '\n')
            {
                return text.ToString();
            }

            if (text.Length >= MaximumRequestCharacters)
            {
                // Terminate rather than buffer or drain an unbounded hostile stream.
                throw new McpFailure($"MCP request exceeds the {MaximumRequestCharacters}-character limit.");
            }

            text.Append((char)character);
        }

        return text.Length == 0 ? null : text.ToString();
    }

    object Dispatch(string method, JsonElement parameters)
    {
        if (method == "ping")
        {
            return new { };
        }

        if (method == "initialize")
        {
            if (_initialized)
            {
                throw new McpFailure("The connection is already initialized.", -32600);
            }

            _ = McpJson.RequiredString(parameters, "protocolVersion");
            if (!parameters.TryGetProperty("capabilities", out var capabilities) || capabilities.ValueKind != JsonValueKind.Object ||
                !parameters.TryGetProperty("clientInfo", out var clientInfo) || clientInfo.ValueKind != JsonValueKind.Object)
            {
                throw new McpFailure("initialize requires capabilities and clientInfo objects.", -32602);
            }

            _ = McpJson.RequiredString(clientInfo, "name");
            _ = McpJson.RequiredString(clientInfo, "version");
            _initialized = true;
            return new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { tools = new { listChanged = false } },
                serverInfo = new { name = "cratis.screenplay", version = typeof(McpConnection).Assembly.GetName().Version!.ToString() },
                instructions = "Read full Screenplay syntax, discover syntax-schema, open a revision-bound workspace, and use read-ast handles with propose-ast for typed edits. Source authoring acceptance is separate from executable readiness. Review exact bytes with read-proposal; identity state persists on apply, and export-workspace is optional for portable transfer or backup. Only apply and explicit recover-workspace may write source. The root must be trusted and exclusively owned during apply or recovery; rollback is not crash-atomic."
            };
        }

        if (!_ready)
        {
            throw new McpFailure("Initialize and send notifications/initialized before using tools.", -32600);
        }

        return method switch
        {
            "tools/list" => parameters.TryGetProperty("cursor", out _) ? throw new McpFailure("This server has no additional tool pages.", -32602) : new { tools = McpToolCatalog.Describe() },
            "tools/call" => tools.Call(parameters),
            _ => throw new McpFailure($"Unknown method '{method}'.", -32601)
        };
    }
}
