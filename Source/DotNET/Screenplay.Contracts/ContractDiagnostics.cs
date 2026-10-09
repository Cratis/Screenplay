// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Contracts;

internal static class ContractDiagnostics
{
    internal static IReadOnlySet<string> Retired => CompilerContractCatalog.Diagnostics.Where(fact => fact.Retired).Select(fact => fact.Code).ToHashSet(StringComparer.Ordinal);

    internal static JsonArray Create()
    {
        var result = new JsonArray();
        foreach (var fact in CompilerContractCatalog.Diagnostics)
        {
            if (string.IsNullOrWhiteSpace(fact.Title) || fact.Severities.Count == 0) throw new InvalidScreenplayContract($"{fact.Code} lacks a catalog title or severity.");
            var names = fact.Severities.OrderDescending().Select(severity => severity.ToString().ToLowerInvariant()).ToArray();
            result.Add(new JsonObject
            {
                ["code"] = fact.Code,
                ["name"] = fact.Name,
                ["severity"] = names[0],
                ["severities"] = new JsonArray([.. names.Select(name => (JsonNode?)JsonValue.Create(name))]),
                ["title"] = fact.Title,
                ["reserved"] = fact.Reserved,
                ["retired"] = fact.Retired
            });
        }

        return result;
    }
}
