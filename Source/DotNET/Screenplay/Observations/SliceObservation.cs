// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Groups observations by model slice; only the event's owning slice holds append counts.
/// </summary>
/// <param name="Slice">The slice identity.</param>
/// <param name="Address">The slice address.</param>
/// <param name="Kind">The slice kind.</param>
/// <param name="Events">Owned event evidence.</param>
/// <param name="Producers">Attributed production evidence.</param>
/// <param name="Consumers">Consumer evidence.</param>
public sealed record SliceObservation(SemanticId Slice, SemanticAddress? Address, SemanticSliceKind Kind,
    ImmutableArray<SliceEventObservation> Events, ImmutableArray<SliceProducerObservation> Producers, ImmutableArray<SliceConsumerObservation> Consumers);

/// <summary>
/// Describes appends for an owned event across all observed generations.
/// </summary>
/// <param name="Event">The event declaration.</param>
/// <param name="TypeId">The mapped stored type identifier.</param>
/// <param name="GenerationCounts">Append counts across observed generations.</param>
/// <param name="Registered">Whether the current declared generation is registered.</param>
public sealed record SliceEventObservation(SemanticId Event, string? TypeId, ImmutableArray<ObservedAppend> GenerationCounts, bool Registered);

/// <summary>
/// Describes one producer's declared event relationships.
/// </summary>
/// <param name="Construct">The command or reaction declaration.</param>
/// <param name="Kind">The producer kind.</param>
/// <param name="Edges">The declared production evidence.</param>
public sealed record SliceProducerObservation(SemanticId Construct, ObservedProducerKind Kind, ImmutableArray<SliceEdgeObservation> Edges);

/// <summary>
/// Describes events attributed to a declared production, not producer invocation counts.
/// </summary>
/// <param name="Event">The event declaration.</param>
/// <param name="AttributedEvents">The event count.</param>
/// <param name="Outcome">The comparison outcome.</param>
/// <param name="Reason">The optional comparison limitation.</param>
public sealed record SliceEdgeObservation(SemanticId Event, long AttributedEvents, ObservationOutcome Outcome, ObservationUnknownReason? Reason);

/// <summary>
/// Identifies a matching subscription and optional progress.
/// </summary>
/// <param name="Identifier">The consumer identifier.</param>
/// <param name="SequenceId">The subscribed sequence.</param>
/// <param name="NextSequenceNumber">The next event to handle.</param>
/// <param name="LastHandledSequenceNumber">The last handled event.</param>
public sealed record ConsumerReference(string Identifier, string SequenceId, long? NextSequenceNumber, long? LastHandledSequenceNumber);

/// <summary>
/// Describes consumer evidence for a modeled trigger event.
/// </summary>
/// <param name="Construct">The consuming declaration.</param>
/// <param name="Event">The trigger event declaration.</param>
/// <param name="References">Matching subscriptions.</param>
/// <param name="Outcome">The comparison outcome.</param>
/// <param name="Reason">The optional comparison limitation.</param>
public sealed record SliceConsumerObservation(SemanticId Construct, SemanticId Event, ImmutableArray<ConsumerReference> References,
    ObservationOutcome Outcome, ObservationUnknownReason? Reason);
