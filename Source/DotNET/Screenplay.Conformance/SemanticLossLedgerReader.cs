// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Reads a loss ledger, collecting every schema problem instead of stopping at the first.
/// </summary>
internal sealed class SemanticLossLedgerReader
{
    static readonly string[] _ledgerProperties = ["schema", "target", "corpusVersion", "minimumFixtureCount", "budget", "excludedFixtures", "losses"];
    static readonly string[] _budgetProperties = ["maximumEntries", "maximumByDesign", "maximumExclusions"];
    static readonly string[] _entryProperties = ["fixture", "change", "path", "direction", "diagnostics", "issue", "byDesign"];

    readonly List<string> _problems = [];

    SemanticLossLedgerReader()
    {
    }

    /// <summary>
    /// Reads a ledger.
    /// </summary>
    /// <param name="stream">The ledger JSON.</param>
    /// <returns>The ledger, with any problems found.</returns>
    public static SemanticLossLedger Read(Stream stream)
    {
        var reader = new SemanticLossLedgerReader();
        try
        {
            using var document = JsonDocument.Parse(stream);
            return reader.Ledger(document.RootElement);
        }
        catch (JsonException exception)
        {
            reader._problems.Add($"The ledger is not valid JSON: {exception.Message}");
            return reader.Empty();
        }
    }

    static T? Parse<T>(string? value)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _) ? parsed : null;

    static string? String(JsonElement element, string property) =>
        SemanticSiblingSegments.StringProperty(element, property);

    SemanticLossLedger Ledger(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            _problems.Add("The ledger must be a JSON object.");
            return Empty();
        }

        Known(root, _ledgerProperties, "the ledger");
        if (!string.Equals(String(root, "schema"), SemanticLossLedger.Schema, StringComparison.Ordinal))
        {
            _problems.Add($"The ledger must declare the schema '{SemanticLossLedger.Schema}'.");
        }

        return new(
            Required(root, "target", "the ledger"),
            Required(root, "corpusVersion", "the ledger"),
            Count(root, "minimumFixtureCount", "the ledger"),
            Budget(root),
            Exclusions(root),
            Losses(root),
            [.. _problems]);
    }

    SemanticLossLedger Empty() => new(string.Empty, string.Empty, 0, new(0, 0, 0), ImmutableSortedDictionary<string, string>.Empty, [], [.. _problems]);

    SemanticLossBudget Budget(JsonElement root)
    {
        if (!root.TryGetProperty("budget", out var budget) || budget.ValueKind != JsonValueKind.Object)
        {
            _problems.Add("The ledger must have a 'budget' object.");
            return new(0, 0, 0);
        }

        Known(budget, _budgetProperties, "the budget");
        return new(Count(budget, "maximumEntries", "the budget"), Count(budget, "maximumByDesign", "the budget"), Count(budget, "maximumExclusions", "the budget"));
    }

    ImmutableSortedDictionary<string, string> Exclusions(JsonElement root)
    {
        var exclusions = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        if (!root.TryGetProperty("excludedFixtures", out var excluded))
        {
            return exclusions.ToImmutable();
        }

        if (excluded.ValueKind != JsonValueKind.Object)
        {
            _problems.Add("'excludedFixtures' must be an object of fixture names to reasons.");
            return exclusions.ToImmutable();
        }

        foreach (var exclusion in excluded.EnumerateObject())
        {
            if (exclusion.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(exclusion.Value.GetString()))
            {
                _problems.Add($"The exclusion of '{exclusion.Name}' must give a reason.");
                continue;
            }

            exclusions[exclusion.Name] = exclusion.Value.GetString()!;
        }

        return exclusions.ToImmutable();
    }

    ImmutableArray<SemanticLossEntry> Losses(JsonElement root)
    {
        if (!root.TryGetProperty("losses", out var losses) || losses.ValueKind != JsonValueKind.Array)
        {
            _problems.Add("The ledger must have a 'losses' array.");
            return [];
        }

        ImmutableArray<SemanticLossEntry> entries = [.. losses.EnumerateArray().Select((entry, index) => Entry(entry, $"loss {index}")).OfType<SemanticLossEntry>()];
        _problems.AddRange(SemanticLossEntryShape.Problems(entries));
        return entries;
    }

    SemanticLossEntry? Entry(JsonElement entry, string where)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            _problems.Add($"The {where} must be an object.");
            return null;
        }

        Known(entry, _entryProperties, $"the {where}");
        var change = Parse<SemanticLossChange>(String(entry, "change"));
        if (change is null)
        {
            _problems.Add($"The {where} has an unknown 'change'.");
        }

        var direction = Parse<RoundTripDirection>(String(entry, "direction"));
        if (direction is null && entry.TryGetProperty("direction", out _))
        {
            _problems.Add($"The {where} has an unknown 'direction'.");
        }

        var diagnostics = entry.TryGetProperty("diagnostics", out var codes) && codes.ValueKind == JsonValueKind.Array
            ? codes.EnumerateArray().Where(_ => _.ValueKind == JsonValueKind.String).Select(_ => _.GetString()!).ToImmutableArray()
            : [];

        return new(Required(entry, "fixture", $"the {where}"), change, String(entry, "path"), direction, diagnostics, String(entry, "issue"), String(entry, "byDesign"));
    }

    void Known(JsonElement element, string[] known, string where)
    {
        foreach (var property in element.EnumerateObject().Where(property => !known.Contains(property.Name, StringComparer.Ordinal)))
        {
            _problems.Add($"Unknown field '{property.Name}' in {where}.");
        }
    }

    string Required(JsonElement element, string property, string where)
    {
        if (String(element, property) is { Length: > 0 } value)
        {
            return value;
        }

        _problems.Add($"Missing '{property}' in {where}.");
        return string.Empty;
    }

    int Count(JsonElement element, string property, string where)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var count) && count >= 0)
        {
            return count;
        }

        _problems.Add($"'{property}' in {where} must be a non-negative whole number.");
        return 0;
    }
}
