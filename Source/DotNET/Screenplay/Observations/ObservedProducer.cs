// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Defines the kind of innermost named producer attributed by the host.
/// </summary>
public enum ObservedProducerKind
{
    /// <summary>
    /// A command.
    /// </summary>
    Command,

    /// <summary>
    /// A reaction.
    /// </summary>
    Reaction
}

/// <summary>
/// Identifies an attributed producer without retaining causation properties.
/// </summary>
/// <param name="Kind">The producer kind.</param>
/// <param name="Identifier">The host's producer identifier.</param>
public sealed record ObservedProducer(ObservedProducerKind Kind, string Identifier);

/// <summary>
/// Counts events attributed to an innermost named producer.
/// </summary>
/// <param name="Producer">The attributed producer.</param>
/// <param name="Type">The appended type and generation.</param>
/// <param name="Events">The event count, never an invocation count.</param>
public sealed record ObservedEdge(ObservedProducer Producer, ObservedEventType Type, long Events);
