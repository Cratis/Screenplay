// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

sealed class McpConnection(McpTools tools, McpAppResources apps)
{
    internal const int MaximumRequestCharacters = 32 * 1024 * 1024;
    internal const string RootsRequestId = "screenplay-roots";
    const string Instructions = "Read full Screenplay syntax, discover syntax-schema, open a revision-bound workspace, and use propose-source for whole .play text documents or read-ast handles with propose-ast for typed edits. Both default to Authoring validation with explicit formatting consent and identity continuity; propose remains executable-only. Parse failures from propose-source return SourceParseFailed with located authoringDiagnostics and no proposal. InvalidIdentityMigration refusals return identityMigrationIssues with exact kind/parts addresses and argument arrays; choose renames for continuity or retirements for removed declarations. Events require both semantic and event-contract migrations. Readiness separates verdicts: readiness.state empty is a valid start, readiness.authoringAccepted is the authoring verdict, and executableReady describes the current ESM executable subset only. A dynamic server picks its root at open-workspace: an explicit path wins, then the client's single project root, where it discovers nested .play files, else Source or src, else a new Screenplay folder. Existing identity or pending-journal state along that discovery path preserves its workspace root. With multiple client roots, pass path to choose one. Without client roots, use the working directory when it holds .play files or a .screenplay folder, otherwise Documents/Screenplay in the user's home folder. Review exact bytes with read-proposal; identity state persists on apply, and export-workspace is optional for portable transfer or backup. Only apply and explicit recover-workspace may write source; both are journaled and recoverable, so hosts need not confirm them each time. The root must be trusted and exclusively owned during apply or recovery; rollback is not crash-atomic.";

    // The rules an assistant otherwise learns only by being rejected, stated before it writes the first proposal.
    const string ModelingInstructions = " Model so the first proposal holds: a specification that checks one read-model instance (given readmodel, then readmodel, then no readmodel, then query) needs exactly one keyed query returning that read model in its slice, such as 'query BookById => Book optional' with 'by bookId BookId'; an event given or appended 'for' an identifier needs a command that produces it for that identifier type; a specification of an authorized command or query needs 'given caller'. An accepted proposal lists introducedExecutableErrors; fix them before apply.";

    const string VisualInstructions = " This host renders views: visualize-model draws the application as an event model board. Pass a proposalId to show what a proposal would change before apply, or sketch documents to draw a what-if that is never written.";

    bool _initialized;
    bool _ready;
    bool _visual;
    bool _clientSupportsRoots;
    bool _fetchingRoots;
    ImmutableArray<string> _clientRootUris = [];

    internal McpConnection(McpTools tools)
        : this(tools, McpAppResources.FromAssembly())
    {
    }

    // Where a failure that has no request to answer is reported; MCP hosts capture the server's stderr in their logs.
    internal TextWriter Log { get; set; } = Console.Error;

    internal void Run(TextReader input, TextWriter output)
    {
        while (ReadLine(input) is { } line)
        {
            var response = Handle(line, input, output);
            if (response is not null)
            {
                output.WriteLine(response);
                output.Flush();
            }
        }
    }

    internal string? Handle(string line, TextReader? input = null, TextWriter? output = null)
    {
        object? id = null;
        var notification = false;
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
                notification = true;
                if (name == "notifications/initialized" && _initialized)
                {
                    _ready = true;
                    FetchClientRoots(input, output);
                }

                if (name == "notifications/roots/list_changed" && _ready)
                {
                    RefetchClientRoots(input, output);
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
            return Error(null, -32700, "Invalid JSON.", "InvalidJson");
        }
        catch (McpFailure failure) when (notification)
        {
            return Unanswered(failure.Message);
        }
        catch (McpFailure failure)
        {
            return Error(id, failure.Code == 0 ? -32603 : failure.Code, failure.Message, failure.FailureKind);
        }
        catch (Exception exception) when (notification)
        {
            return Unanswered(exception.Message);
        }
        catch (Exception exception)
        {
            // Protocol errors remain protocol messages; a failed request never terminates as a success.
            return Error(id, -32603, $"Request failed: {exception.Message}", "RequestFailed");
        }
    }

    static string Error(object? id, int code, string message, string failureKind) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message, data = new { failureKind } } }, McpJson.Options);

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
            _clientSupportsRoots = capabilities.TryGetProperty("roots", out var rootsCapability) && rootsCapability.ValueKind == JsonValueKind.Object;

            // Views are offered only to a host that renders them; every other client sees the same server as before.
            _visual = apps.Available && McpAppResources.Supports(capabilities);
            return new
            {
                protocolVersion = "2025-06-18",
                capabilities = _visual
                    ? new Dictionary<string, object>
                    {
                        ["tools"] = new { listChanged = false },
                        ["resources"] = new { listChanged = false },
                        ["extensions"] = new Dictionary<string, object> { [McpAppResources.Extension] = new { } }
                    }
                    : new Dictionary<string, object> { ["tools"] = new { listChanged = false } },
                serverInfo = new { name = "cratis.screenplay", version = typeof(McpConnection).Assembly.GetName().Version!.ToString() },
                instructions = Instructions + ModelingInstructions + (_visual ? VisualInstructions : string.Empty)
            };
        }

        if (!_ready)
        {
            throw new McpFailure("Initialize and send notifications/initialized before using tools.", -32600);
        }

        return method switch
        {
            "tools/list" => parameters.TryGetProperty("cursor", out _) ? throw new McpFailure("This server has no additional tool pages.", -32602) : new { tools = McpToolCatalog.Describe(_visual) },
            "tools/call" => tools.Call(parameters, _visual),
            "resources/list" when _visual => parameters.TryGetProperty("cursor", out _) ? throw new McpFailure("This server has no additional resource pages.", -32602) : apps.List(),
            "resources/templates/list" when _visual => new { resourceTemplates = Array.Empty<object>() },
            "resources/read" when _visual => apps.Read(parameters),
            _ => throw new McpFailure($"Unknown method '{method}'.", -32601)
        };
    }

    // A notification has no id to answer, and an error with a null id makes MCP hosts drop the connection,
    // so a failed notification is reported to the log and the session continues.
    string? Unanswered(string message)
    {
        Log.WriteLine($"Screenplay MCP: a notification failed and was not answered: {message}");
        Log.Flush();
        return null;
    }

    // Asks a host that advertises the roots capability which roots it offers, so a dynamic server can pick
    // one at open-workspace time. Runs once after initialization and again whenever the host reports change.
    void FetchClientRoots(TextReader? input, TextWriter? output)
    {
        if (_fetchingRoots || input is null || output is null || !_clientSupportsRoots || !tools.DynamicRoot)
        {
            return;
        }

        _fetchingRoots = true;
        try
        {
            var uris = new List<string>();
            string? cursor = null;
            do
            {
                var request = new Dictionary<string, object>
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = RootsRequestId,
                    ["method"] = "roots/list",
                    ["params"] = cursor is null ? [] : new Dictionary<string, object> { ["cursor"] = cursor }
                };
                output.WriteLine(JsonSerializer.Serialize(request, McpJson.Options));
                output.Flush();

                JsonElement response;
                while (true)
                {
                    var line = ReadLine(input) ?? throw new McpFailure("The client closed the connection while answering roots/list.");
                    using var document = JsonDocument.Parse(line);
                    var candidate = document.RootElement.Clone();
                    if (candidate.ValueKind == JsonValueKind.Object &&
                        candidate.TryGetProperty("id", out var identifier) && identifier.ValueKind == JsonValueKind.String &&
                        identifier.GetString() == RootsRequestId && !candidate.TryGetProperty("method", out _))
                    {
                        response = candidate;
                        break;
                    }

                    // Another client message arrived while the answer was pending; answer it and keep waiting.
                    var forwarded = Handle(line, input, output);
                    if (forwarded is not null)
                    {
                        output.WriteLine(forwarded);
                        output.Flush();
                    }
                }

                if (response.TryGetProperty("error", out var failure))
                {
                    throw new McpFailure($"The client refused roots/list: {failure.GetRawText()}");
                }

                var result = response.GetProperty("result");
                if (!result.TryGetProperty("roots", out var infos) || infos.ValueKind != JsonValueKind.Array)
                {
                    throw new McpFailure("The client answered roots/list without a roots array.");
                }

                foreach (var info in infos.EnumerateArray())
                {
                    uris.Add(McpJson.RequiredString(info, "uri"));
                }

                cursor = result.TryGetProperty("nextCursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
            }
            while (cursor is not null);

            tools.ClientRoots = [.. uris];
            _clientRootUris = [.. uris];
        }
        finally
        {
            _fetchingRoots = false;
        }
    }

    void RefetchClientRoots(TextReader? input, TextWriter? output)
    {
        var derived = tools.ClientDerivedRootPath;
        FetchClientRoots(input, output);
        if (derived is null)
        {
            return;
        }

        // A binding that came from the client's roots survives only while the host still offers it.
        var stillOffered = _clientRootUris.Any(uri =>
            Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
            string.Equals(parsed.Scheme, "file", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Uri.UnescapeDataString(parsed.AbsolutePath), derived, StringComparison.OrdinalIgnoreCase));
        if (!stillOffered)
        {
            tools.UnbindClientRoot(derived);
        }
    }
}
