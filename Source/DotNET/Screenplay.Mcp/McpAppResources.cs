// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// The MCP Apps views the server offers: user interfaces a host renders beside the conversation.
/// </summary>
/// <param name="boardHtml">The event model board page, or <see langword="null"/> when it was not built into the server.</param>
sealed class McpAppResources(string? boardHtml)
{
    /// <summary>The MCP Apps extension identifier, negotiated through the extensions capability.</summary>
    internal const string Extension = "io.modelcontextprotocol/ui";

    /// <summary>The MIME type of an MCP Apps HTML view.</summary>
    internal const string MimeType = "text/html;profile=mcp-app";

    /// <summary>The address of the event model board view.</summary>
    internal const string BoardUri = "ui://screenplay/event-model-board.html";

    /// <summary>Where the board page is embedded in this assembly.</summary>
    internal const string BoardResourceName = "Cratis.Screenplay.Mcp.Apps.event-model-board.html";

    // The board loads its icon font from here; the host's sandbox blocks every other origin.
    static readonly string[] _resourceDomains = ["https://cdn.jsdelivr.net"];

    /// <summary>Gets whether the board view is available to offer.</summary>
    internal bool Available => boardHtml is not null;

    /// <summary>
    /// Reads the views embedded in this assembly.
    /// </summary>
    /// <returns>The views, without the board when it was not built into the server.</returns>
    internal static McpAppResources FromAssembly()
    {
        using var stream = typeof(McpAppResources).Assembly.GetManifestResourceStream(BoardResourceName);
        if (stream is null)
        {
            return new(null);
        }

        using var reader = new StreamReader(stream);
        return new(reader.ReadToEnd());
    }

    /// <summary>
    /// Decides whether a client renders MCP Apps HTML views, from the capabilities it initialized with.
    /// </summary>
    /// <param name="capabilities">The client's capabilities.</param>
    /// <returns><see langword="true"/> when the client advertises the extension and accepts HTML views.</returns>
    internal static bool Supports(JsonElement capabilities)
    {
        if (!capabilities.TryGetProperty("extensions", out var extensions) || extensions.ValueKind != JsonValueKind.Object ||
            !extensions.TryGetProperty(Extension, out var extension) || extension.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!extension.TryGetProperty("mimeTypes", out var mimeTypes))
        {
            return true;
        }

        return mimeTypes.ValueKind == JsonValueKind.Array &&
            mimeTypes.EnumerateArray().Any(mimeType => mimeType.ValueKind == JsonValueKind.String && mimeType.GetString() == MimeType);
    }

    /// <summary>
    /// Lists the views as MCP resources.
    /// </summary>
    /// <returns>The resources/list result.</returns>
    internal object List() => new
    {
        resources = Available
            ? new[] { new { uri = BoardUri, name = "event-model-board", title = "Event model board", description = "Draws a Screenplay application as an event model board, and what a proposed or sketched change would make of it.", mimeType = MimeType } }
            : []
    };

    /// <summary>
    /// Reads one view.
    /// </summary>
    /// <param name="parameters">The resources/read parameters.</param>
    /// <returns>The resources/read result.</returns>
    /// <exception cref="McpFailure">The resource is not one this server offers.</exception>
    internal object Read(JsonElement parameters)
    {
        McpJson.ValidateObject(parameters, ["uri", "_meta"], ["uri"]);
        var uri = McpJson.RequiredString(parameters, "uri");
        if (uri != BoardUri || boardHtml is null)
        {
            throw new McpFailure($"Resource '{uri}' was not found.", -32002);
        }

        var content = new Dictionary<string, object>
        {
            ["uri"] = BoardUri,
            ["mimeType"] = MimeType,
            ["text"] = boardHtml,
            ["_meta"] = new { ui = new { csp = new { resourceDomains = _resourceDomains }, prefersBorder = false } }
        };
        return new { contents = new[] { content } };
    }
}
