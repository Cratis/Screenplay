// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Completeness;

/// <summary>
/// Selects the structural checks a caller wants to run.
/// </summary>
/// <param name="Selected">The selected checks.</param>
public sealed record CompletenessChecks(ImmutableHashSet<CompletenessCheck> Selected)
{
    /// <summary>
    /// Gets the empty selection.
    /// </summary>
    public static CompletenessChecks None { get; } = new([]);

    /// <summary>
    /// Gets every check.
    /// </summary>
    public static CompletenessChecks All { get; } = new([.. Enum.GetValues<CompletenessCheck>()]);

    /// <summary>
    /// Gets every accepted selection name, including aliases and <c>all</c>.
    /// </summary>
    public static ImmutableArray<string> AcceptedNames => [.. Names.Keys.Append("all").Order(StringComparer.Ordinal)];

    static ImmutableDictionary<string, CompletenessCheck> Names { get; } = CreateNames();

    /// <summary>
    /// Gets the canonical kebab-case name of a check.
    /// </summary>
    /// <param name="check">The check.</param>
    /// <returns>The name used in effective gate reports.</returns>
    public static string NameOf(CompletenessCheck check) => Names.First(pair => pair.Value == check && !pair.Key.StartsWith("PLAY", StringComparison.Ordinal)).Key;

    /// <summary>
    /// Parses comma-separated kebab-case names, diagnostic codes, or <c>all</c>.
    /// </summary>
    /// <param name="value">The selection to parse.</param>
    /// <param name="checks">The selection, or <see cref="None"/> on failure.</param>
    /// <returns>Whether every token is recognized.</returns>
    public static bool TryParse(string value, out CompletenessChecks checks)
    {
        checks = None;
        foreach (var token in value.Split(',').Select(token => token.Trim()))
        {
            if (token == "all")
            {
                checks = All;
                continue;
            }

            if (!Names.TryGetValue(token, out var check))
            {
                checks = None;
                return false;
            }

            checks = new(checks.Selected.Add(check));
        }

        return true;
    }

    static ImmutableDictionary<string, CompletenessCheck> CreateNames()
    {
        var groups = new (CompletenessCheck Check, string[] Names)[]
        {
            (CompletenessCheck.DataBindings, ["data-bindings", "PLAY0530", "PLAY0531"]),
            (CompletenessCheck.InputSurfaces, ["input-surfaces", "PLAY0532", "PLAY0533"]),
            (CompletenessCheck.FieldOrigins, ["field-origins", "PLAY0534"]),
            (CompletenessCheck.QueryKeys, ["query-keys", "PLAY0535"]),
            (CompletenessCheck.EventConsumers, ["event-consumers", "PLAY0536"]),
            (CompletenessCheck.Navigation, ["navigation", "PLAY0537"]),
            (CompletenessCheck.Personas, ["personas", "PLAY0574", "PLAY0575", "PLAY0576"]),
            (CompletenessCheck.Purposes, ["purposes", "PLAY0602", "PLAY0603", "PLAY0604", "PLAY0605", "PLAY0606"]),
            (CompletenessCheck.Privilege, ["privilege", "PLAY0652"]),
            (CompletenessCheck.EnumPinning, ["enum-pinning", "PLAY0671"])
        };
        return groups.SelectMany(group => group.Names.Select(name => KeyValuePair.Create(name, group.Check))).ToImmutableDictionary(StringComparer.Ordinal);
    }
}
