// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpNotesArguments
{
    internal static string State(JsonElement arguments) => Selection(arguments, "state", "open", ["open", "resolved", "orphaned", "all"]);
    internal static string View(JsonElement arguments) => Selection(arguments, "view", "items", ["items", "findings"]);

    static string Selection(JsonElement arguments, string name, string fallback, string[] choices)
    {
        var value = McpJson.OptionalString(arguments, name) ?? fallback;
        if (!choices.Contains(value, StringComparer.Ordinal)) throw new McpFailure($"Unsupported value for '{name}'.", -32602);
        return value;
    }
}
