// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpIdentityChanges
{
    internal static ImmutableArray<SemanticIdentityRename> SemanticRenames(JsonElement arguments) =>
        [.. Renames(arguments, "semanticRenames").Select(rename => new SemanticIdentityRename(rename.Previous, rename.Current))];

    internal static ImmutableArray<EventContractIdentityRename> EventRenames(JsonElement arguments) =>
        [.. Renames(arguments, "eventRenames").Select(rename => new EventContractIdentityRename(rename.Previous, rename.Current))];

    internal static ImmutableArray<SemanticAddress> RetiredSemanticAddresses(JsonElement arguments) => Addresses(arguments, "retiredSemanticAddresses");

    internal static ImmutableArray<SemanticAddress> RetiredEventAddresses(JsonElement arguments) => Addresses(arguments, "retiredEventAddresses");

    static IEnumerable<(SemanticAddress Previous, SemanticAddress Current)> Renames(JsonElement arguments, string name)
    {
        foreach (var value in Values(arguments, name))
        {
            McpJson.ValidateObject(value, ["previousAddress", "currentAddress"], ["previousAddress", "currentAddress"]);
            yield return (McpSemanticAddresses.Read(value.GetProperty("previousAddress")), McpSemanticAddresses.Read(value.GetProperty("currentAddress")));
        }
    }

    static ImmutableArray<SemanticAddress> Addresses(JsonElement arguments, string name) =>
        [.. Values(arguments, name).Select(McpSemanticAddresses.Read)];

    static JsonElement.ArrayEnumerator Values(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var values))
        {
            return McpJson.EmptyArray.EnumerateArray();
        }

        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() > McpWorkspaceOperations.MaximumOperations)
        {
            throw new McpFailure($"'{name}' must contain at most {McpWorkspaceOperations.MaximumOperations} entries.", -32602);
        }

        return values.EnumerateArray();
    }
}
