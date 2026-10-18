// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Records bounded evidence and its human-readable statement in one shared result.
/// </summary>
/// <param name="Kind">The finding kind.</param>
/// <param name="Outcome">The evidence outcome.</param>
/// <param name="Statement">The stable explanatory statement.</param>
/// <param name="Reason">The optional limitation or exclusion reason.</param>
/// <param name="Construct">The model declaration identity.</param>
/// <param name="Address">The model declaration address.</param>
/// <param name="Slice">The owning slice identity.</param>
/// <param name="EventTypeId">The stored event type identifier.</param>
/// <param name="Generation">The event generation.</param>
/// <param name="Producer">The attributed producer.</param>
/// <param name="Consumer">The consumer identifier.</param>
/// <param name="SequenceId">The observed sequence.</param>
/// <param name="Count">The event count, never an invocation count.</param>
public sealed record ObservationFinding(
    ObservationFindingKind Kind,
    ObservationOutcome Outcome,
    string Statement,
    ObservationUnknownReason? Reason = null,
    SemanticId? Construct = null,
    SemanticAddress? Address = null,
    SemanticId? Slice = null,
    string? EventTypeId = null,
    uint? Generation = null,
    ObservedProducer? Producer = null,
    string? Consumer = null,
    string? SequenceId = null,
    long? Count = null);
