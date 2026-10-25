// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Checks the rules about a run as a whole: the budget (R6), non-vacuity (R7) and the exclusions.
/// </summary>
internal static class SemanticLossRunRules
{
    /// <summary>
    /// Finds every budget the ledger exceeds.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> OverBudget(SemanticLossLedger ledger)
    {
        var byDesign = ledger.Losses.Count(entry => entry.ByDesign is not null);
        if (ledger.Losses.Length > ledger.Budget.MaximumEntries)
        {
            yield return Run($"The ledger has {Count(ledger.Losses.Length)} entries; the budget allows {Count(ledger.Budget.MaximumEntries)}.");
        }

        if (byDesign > ledger.Budget.MaximumByDesign)
        {
            yield return Run($"The ledger accepts {Count(byDesign)} losses by design; the budget allows {Count(ledger.Budget.MaximumByDesign)}.");
        }

        if (ledger.ExcludedFixtures.Count > ledger.Budget.MaximumExclusions)
        {
            yield return Run($"The ledger excludes {Count(ledger.ExcludedFixtures.Count)} fixtures; the budget allows {Count(ledger.Budget.MaximumExclusions)}.");
        }
    }

    /// <summary>
    /// Finds every reason the run proves nothing.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="executed">The number of fixtures observed.</param>
    /// <param name="compared">The number of observations that were compared.</param>
    /// <param name="requireCompleteRun">Whether the run claims to be complete.</param>
    /// <param name="eligible">The eligible fixtures, when given.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> Vacuous(SemanticLossLedger ledger, int executed, int compared, bool requireCompleteRun, IReadOnlySet<string>? eligible)
    {
        if (executed == 0)
        {
            yield return Run("No fixture was observed.");
        }

        if (!requireCompleteRun)
        {
            yield break;
        }

        if (eligible is null)
        {
            yield return Run("A complete run must name its eligible fixtures.");
        }

        if (executed < ledger.MinimumFixtureCount)
        {
            yield return Run($"A complete run observed {Count(executed)} fixtures; the ledger requires at least {Count(ledger.MinimumFixtureCount)}.");
        }

        if (compared == 0)
        {
            yield return Run("No observation was compared; every recovered source failed to compile.");
        }
    }

    /// <summary>
    /// Finds the exclusions naming a fixture that was observed.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="observed">The observed fixtures.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> ObsoleteExclusions(SemanticLossLedger ledger, IReadOnlySet<string> observed) =>
        ledger.ExcludedFixtures.Keys.Where(observed.Contains)
            .Select(fixture => new SemanticLossFinding(fixture, null, null, "The fixture is excluded but was observed; remove the exclusion."));

    /// <summary>
    /// Finds the exclusions naming a fixture that is not eligible.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="eligible">The eligible fixtures.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> IneligibleExclusions(SemanticLossLedger ledger, IReadOnlySet<string> eligible) =>
        ledger.ExcludedFixtures.Keys.Where(fixture => !eligible.Contains(fixture))
            .Select(fixture => new SemanticLossFinding(fixture, null, null, "The fixture is excluded but is not an eligible fixture."));

    /// <summary>
    /// Finds the entries whose fixture a complete run did not observe.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="observed">The observed fixtures.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> Orphans(SemanticLossLedger ledger, IReadOnlySet<string> observed) =>
        ledger.Losses.Where(entry => !observed.Contains(entry.Fixture))
            .Select(entry => new SemanticLossFinding(
                entry.Fixture,
                entry.Change,
                entry.Path,
                ledger.ExcludedFixtures.ContainsKey(entry.Fixture) ? "The entry is for an excluded fixture." : "The entry is for a fixture the complete run did not observe."));

    /// <summary>
    /// Finds the eligible fixtures that were neither observed nor excluded.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="observed">The observed fixtures.</param>
    /// <param name="eligible">The eligible fixtures.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<SemanticLossFinding> Unobserved(SemanticLossLedger ledger, IReadOnlySet<string> observed, IReadOnlySet<string> eligible) =>
        eligible.Where(fixture => !observed.Contains(fixture) && !ledger.ExcludedFixtures.ContainsKey(fixture))
            .Order(StringComparer.Ordinal)
            .Select(fixture => new SemanticLossFinding(fixture, null, null, "The fixture is eligible but was neither observed nor excluded."));

    static SemanticLossFinding Run(string message) => new(string.Empty, null, null, message);

    static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
}
