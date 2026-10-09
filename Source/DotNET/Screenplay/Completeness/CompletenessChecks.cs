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

            var check = token switch
            {
                "data-bindings" or "PLAY0530" or "PLAY0531" => CompletenessCheck.DataBindings,
                "input-surfaces" or "PLAY0532" or "PLAY0533" => CompletenessCheck.InputSurfaces,
                "field-origins" or "PLAY0534" => CompletenessCheck.FieldOrigins,
                "query-keys" or "PLAY0535" => CompletenessCheck.QueryKeys,
                "event-consumers" or "PLAY0536" => CompletenessCheck.EventConsumers,
                "navigation" or "PLAY0537" => CompletenessCheck.Navigation,
                "purposes" or "PLAY0578" or "PLAY0579" or "PLAY0580" or "PLAY0581" or "PLAY0582" => CompletenessCheck.Purposes,
                _ => (CompletenessCheck?)null
            };
            if (check is null)
            {
                checks = None;
                return false;
            }

            checks = new(checks.Selected.Add(check.Value));
        }

        return true;
    }
}
