// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

sealed record McpPropertyOptions(string Action, bool NewGeneration, bool NeverPersisted)
{
    internal static McpPropertyOptions Read(JsonElement arguments)
    {
        var options = new McpPropertyOptions(McpJson.RequiredString(arguments, "action"), McpJson.Boolean(arguments, "newGeneration"), McpJson.Boolean(arguments, "eventNeverPersisted"));
        if (options.NewGeneration && options.NeverPersisted) Refuse();
        if (InvalidSelections(options.Action, arguments)) Refuse();
        RequireActionFields(options.Action, arguments);
        return options;
    }

    internal string? NewName(JsonElement arguments) => Action == "rename" ? McpJson.RequiredString(arguments, "newName") : null;

    static void RequireActionFields(string action, JsonElement arguments)
    {
        string[] required = action switch
        {
            "add" => ["owner", "expectedOwnerName", "name", "type"],
            "rename" => ["target", "expectedName", "newName"],
            _ => ["target", "expectedName"]
        };
        foreach (var field in required.Where(field => !arguments.TryGetProperty(field, out _)))
        {
            throw new McpFailure($"Property {action} requires '{field}'.", -32602);
        }
    }

    static bool InvalidSelections(string action, JsonElement arguments) => InvalidActionFields(action, arguments) || action switch
    {
        "rename" => arguments.TryGetProperty("alsoRemove", out _),
        "remove" => arguments.TryGetProperty("alsoRename", out _) || arguments.TryGetProperty("newName", out _),
        _ => false
    };

    static bool InvalidActionFields(string action, JsonElement arguments)
    {
        string[] invalid = action == "add" ? ["target", "expectedName", "newName", "alsoRename", "alsoRemove"] : ["owner", "expectedOwnerName", "name", "type", "optional", "hops", "fixtureValue"];
        return invalid.Any(field => arguments.TryGetProperty(field, out _));
    }

    static void Refuse() => throw new McpFailure("Select one action and at most one event evolution mode.", -32602);
}
