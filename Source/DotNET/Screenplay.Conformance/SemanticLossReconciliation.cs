// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Reconciles round-trip observations against a loss ledger, rule by rule.
/// </summary>
internal static class SemanticLossReconciliation
{
    /// <summary>
    /// Reconciles observations against a ledger.
    /// </summary>
    /// <param name="ledger">The ledger.</param>
    /// <param name="observations">The observations.</param>
    /// <param name="requireCompleteRun">Whether the run claims to be complete.</param>
    /// <param name="eligibleFixtures">The eligible fixtures, when given.</param>
    /// <returns>The verdict.</returns>
    public static SemanticLossVerdict Reconcile(SemanticLossLedger ledger, IEnumerable<RoundTripObservation> observations, bool requireCompleteRun, IEnumerable<string>? eligibleFixtures)
    {
        var all = observations.Select(Defaulted).ToArray();
        var byFixture = all.GroupBy(observation => observation.Fixture, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var observed = byFixture.Keys.ToHashSet(StringComparer.Ordinal);
        var eligible = eligibleFixtures?.ToHashSet(StringComparer.Ordinal);
        var entries = ledger.Losses.Where(entry => entry.Change is not null).ToLookup(entry => entry.Fixture, StringComparer.Ordinal);
        var fixtures = byFixture.Values.Select(observation => Fixture(observation, [.. entries[observation.Fixture]])).ToArray();
        var compared = all.Count(observation => observation.Comparison is not null);

        return new()
        {
            Executed = byFixture.Count,
            Compared = compared,
            Malformed = [.. ledger.Problems.Select(problem => new SemanticLossFinding(string.Empty, null, null, problem)), .. Repeated(all)],
            Unledgered = [.. fixtures.SelectMany(_ => _.Unledgered)],
            Stale = [.. fixtures.SelectMany(_ => _.Stale)],
            Unexplained = [.. fixtures.SelectMany(_ => _.Unexplained), .. Silent(ledger)],
            Uncited = [.. fixtures.SelectMany(_ => _.Uncited)],
            OverBudget = [.. SemanticLossRunRules.OverBudget(ledger)],
            Vacuous = [.. SemanticLossRunRules.Vacuous(ledger, byFixture.Count, compared, requireCompleteRun, eligible)],
            Orphans = requireCompleteRun ? [.. SemanticLossRunRules.Orphans(ledger, observed)] : [],
            IneligibleExclusions = requireCompleteRun && eligible is not null ? [.. SemanticLossRunRules.IneligibleExclusions(ledger, eligible)] : [],
            ObsoleteExclusions = [.. SemanticLossRunRules.ObsoleteExclusions(ledger, observed)],
            Unobserved = requireCompleteRun && eligible is not null ? [.. SemanticLossRunRules.Unobserved(ledger, observed, eligible)] : [],
        };
    }

    static FixtureFindings Fixture(RoundTripObservation observation, SemanticLossEntry[] entries)
    {
        var findings = new FixtureFindings();
        var matched = new HashSet<SemanticLossEntry>(ReferenceEqualityComparer.Instance);
        Differences(observation, entries, matched, findings);
        foreach (var entry in entries.Where(entry => entry.Change == SemanticLossChange.Diagnostic && Occurring(observation, entry).Overlaps(entry.Diagnostics)))
        {
            matched.Add(entry);
        }

        findings.Stale.AddRange(entries.Where(entry => !matched.Contains(entry))
            .Select(entry => new SemanticLossFinding(observation.Fixture, entry.Change, entry.Path, "The entry matches nothing this fixture produced; remove it.")));
        findings.Unexplained.AddRange(entries.SelectMany(entry => Unexplained(observation, entry)));
        findings.Uncited.AddRange(Uncited(observation, entries));
        return findings;
    }

    static void Differences(RoundTripObservation observation, SemanticLossEntry[] entries, HashSet<SemanticLossEntry> matched, FixtureFindings findings)
    {
        if (observation.Comparison is null)
        {
            Uncompilable(observation, entries, matched, findings);
            return;
        }

        foreach (var difference in observation.Comparison.Differences)
        {
            var change = Change(difference.Kind);
            var match = Array.Find(entries, entry => entry.Change == change && string.Equals(entry.Path, difference.Path, StringComparison.Ordinal));
            if (match is null)
            {
                findings.Unledgered.Add(new(observation.Fixture, change, difference.Path, $"Expected {difference.Expected ?? "nothing"}, got {difference.Actual ?? "nothing"}."));
                continue;
            }

            matched.Add(match);
        }
    }

    static IEnumerable<SemanticLossFinding> Unexplained(RoundTripObservation observation, SemanticLossEntry entry)
    {
        var occurring = Occurring(observation, entry);
        return entry.Diagnostics.Where(code => !occurring.Contains(code))
            .Select(code => new SemanticLossFinding(observation.Fixture, entry.Change, entry.Path, $"The entry cites '{code}', which did not occur for its direction."));
    }

    static IEnumerable<SemanticLossFinding> Uncited(RoundTripObservation observation, SemanticLossEntry[] entries) =>
        observation.Diagnostics.Where(diagnostic => diagnostic.Severity >= RoundTripDiagnosticSeverity.Warning)
            .Where(diagnostic => !entries.Any(entry => entry.Direction == diagnostic.Direction && entry.Diagnostics.Contains(diagnostic.Code, StringComparer.Ordinal)))
            .Select(diagnostic => new SemanticLossFinding(observation.Fixture, null, null, $"The {diagnostic.Severity} '{diagnostic.Code}' from {diagnostic.Direction} is cited by no entry."));

    static void Uncompilable(RoundTripObservation observation, SemanticLossEntry[] entries, HashSet<SemanticLossEntry> matched, FixtureFindings findings)
    {
        var uncompilable = entries.Where(entry => entry.Change == SemanticLossChange.Uncompilable).ToArray();
        matched.UnionWith(uncompilable);
        if (uncompilable.Length == 0)
        {
            findings.Unledgered.Add(new(observation.Fixture, SemanticLossChange.Uncompilable, null, "The recovered source did not compile and no entry accepts it."));
        }
    }

    static HashSet<string> Occurring(RoundTripObservation observation, SemanticLossEntry entry)
    {
        var codes = observation.Diagnostics.Where(diagnostic => diagnostic.Direction == entry.Direction).Select(diagnostic => diagnostic.Code).ToHashSet(StringComparer.Ordinal);
        if (entry.Change == SemanticLossChange.Uncompilable)
        {
            codes.UnionWith(observation.RecoveryCompileDiagnostics);
        }

        return codes;
    }

    static RoundTripObservation Defaulted(RoundTripObservation observation) => observation with
    {
        Diagnostics = observation.Diagnostics.IsDefault ? [] : observation.Diagnostics,
        RecoveryCompileDiagnostics = observation.RecoveryCompileDiagnostics.IsDefault ? [] : observation.RecoveryCompileDiagnostics,
    };

    static IEnumerable<SemanticLossFinding> Silent(SemanticLossLedger ledger) =>
        ledger.Losses.Where(entry => entry.Diagnostics.IsDefaultOrEmpty && entry.ByDesign is not null)
            .Select(entry => new SemanticLossFinding(entry.Fixture, entry.Change, entry.Path, "A loss no diagnostic explains is silent; it must carry an issue, never 'byDesign'."));

    static IEnumerable<SemanticLossFinding> Repeated(RoundTripObservation[] observations) =>
        observations.GroupBy(observation => observation.Fixture, StringComparer.Ordinal).Where(group => group.Count() > 1)
            .Select(group => new SemanticLossFinding(group.Key, null, null, "The fixture was observed more than once."));

    static SemanticLossChange Change(SemanticDifferenceKind kind) => kind switch
    {
        SemanticDifferenceKind.Missing => SemanticLossChange.Missing,
        SemanticDifferenceKind.Unexpected => SemanticLossChange.Unexpected,
        _ => SemanticLossChange.Changed,
    };

    sealed class FixtureFindings
    {
        public List<SemanticLossFinding> Unledgered { get; } = [];

        public List<SemanticLossFinding> Stale { get; } = [];

        public List<SemanticLossFinding> Unexplained { get; } = [];

        public List<SemanticLossFinding> Uncited { get; } = [];
    }
}
