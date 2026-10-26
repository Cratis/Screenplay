// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents the reviewed list of semantic losses a round-trip target accepts, and reconciles observed round trips against it.
/// </summary>
/// <param name="Target">The round-trip target, such as <c language="text">cratis-arc</c>.</param>
/// <param name="CorpusVersion">The canonical corpus version the ledger was measured against.</param>
/// <param name="MinimumFixtureCount">The fewest fixtures a complete run must observe.</param>
/// <param name="Budget">The most the ledger may accept.</param>
/// <param name="ExcludedFixtures">The fixtures the target does not round-trip, with the reason for each.</param>
/// <param name="Losses">The accepted losses.</param>
/// <param name="Problems">The problems found while loading; a ledger with problems never reconciles clean.</param>
internal sealed record SemanticLossLedger(
    string Target,
    string CorpusVersion,
    int MinimumFixtureCount,
    SemanticLossBudget Budget,
    ImmutableSortedDictionary<string, string> ExcludedFixtures,
    ImmutableArray<SemanticLossEntry> Losses,
    ImmutableArray<string> Problems)
{
    /// <summary>
    /// The schema identifier a ledger declares.
    /// </summary>
    public const string Schema = "cratis.screenplay.loss-ledger/v1";

    /// <summary>
    /// Loads a ledger. Schema problems are reported in <see cref="Problems"/> rather than thrown.
    /// </summary>
    /// <param name="stream">The ledger JSON.</param>
    /// <returns>The ledger.</returns>
    public static SemanticLossLedger Load(Stream stream) => SemanticLossLedgerReader.Read(stream);

    /// <summary>
    /// Reconciles observed round trips against the ledger.
    /// </summary>
    /// <param name="observations">One observation per fixture that ran.</param>
    /// <param name="requireCompleteRun">
    /// <see langword="true"/> for a run over every eligible fixture, which also applies the minimum fixture count and reports orphans and unobserved
    /// fixtures; <see langword="false"/> for a partial run, such as a single fixture.
    /// </param>
    /// <param name="eligibleFixtures">The fixtures a complete run may observe; a complete run without them is vacuous.</param>
    /// <returns>The verdict.</returns>
    public SemanticLossVerdict Reconcile(IEnumerable<RoundTripObservation> observations, bool requireCompleteRun, IEnumerable<string>? eligibleFixtures = null) =>
        SemanticLossReconciliation.Reconcile(this, observations, requireCompleteRun, eligibleFixtures);
}
