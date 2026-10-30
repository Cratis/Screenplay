// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Context version 1 for a standalone event-trigger reaction body, after its where guard passed.
/// Unavailable run fields are null. There is no read access or sequence number.
/// </summary>
/// <param name="EventType">The modeled triggering event contract.</param>
/// <param name="Event">The typed event payload, keyed by declared property IDs.</param>
/// <param name="EventSourceId">The triggering event's source.</param>
/// <param name="Tenant">The tenant, or null when unavailable.</param>
/// <param name="CausedBy">The audit identity, or null when unavailable.</param>
/// <param name="Causation">The causation, or null when unavailable.</param>
/// <param name="Occurred">The occurrence time, or null when unavailable.</param>
/// <param name="RunsAs">The separately declared system identity, or null when absent.</param>
public sealed record SemanticReactionContext(
    SemanticId EventType,
    SemanticCompositeValue Event,
    SemanticValue EventSourceId,
    string? Tenant,
    SemanticBodyIdentity? CausedBy,
    SemanticValue? Causation,
    DateTimeOffset? Occurred,
    SemanticReactionIdentity? RunsAs);
