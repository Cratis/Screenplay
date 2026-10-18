// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Carries snapshot scope and totals, counted once rather than summed across slices.
/// </summary>
/// <param name="EventStore">The store identifier.</param>
/// <param name="Namespace">The namespace identifier.</param>
/// <param name="Sequence">The sequence scan coverage.</param>
/// <param name="CapturedAt">The host capture time.</param>
/// <param name="AppendedEvents">The total examined append count.</param>
/// <param name="AttributedEvents">The total attributed event count.</param>
/// <param name="UnattributedEvents">The total append count without attributed producers, including unaccounted evidence.</param>
public sealed record ObservationCoverage(string EventStore, string Namespace, ObservedSequenceCoverage Sequence,
    DateTimeOffset CapturedAt, long AppendedEvents, long AttributedEvents, long UnattributedEvents);

/// <summary>
/// Reports agreement of bounded observations with declared relationships, not runtime correctness.
/// </summary>
/// <param name="HasDrift">True for known drift, false only when complete without unknowns, otherwise null.</param>
/// <param name="Complete">Whether scan coverage and comparison evidence are complete.</param>
/// <param name="Coverage">The snapshot scope and totals.</param>
/// <param name="Findings">Findings in stable model order.</param>
/// <param name="Slices">Evidence grouped by owning slice.</param>
/// <param name="NotEstablished">Stable statements of what this comparison cannot establish.</param>
public sealed record ObservationReport(bool? HasDrift, bool Complete, ObservationCoverage Coverage,
    ImmutableArray<ObservationFinding> Findings, ImmutableArray<SliceObservation> Slices, ImmutableArray<string> NotEstablished);
