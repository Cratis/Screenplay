// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Defines a registered consumer's responsibility.
/// </summary>
public enum ObservedConsumerKind
{
    /// <summary>
    /// A reaction consumer.
    /// </summary>
    Reaction,

    /// <summary>
    /// A projection consumer; not compared in this version.
    /// </summary>
    Projection,

    /// <summary>
    /// A reducer consumer; not compared in this version.
    /// </summary>
    Reducer,

    /// <summary>
    /// Another consumer kind.
    /// </summary>
    Other
}

/// <summary>
/// Defines who owns a consumer, independently of its kind.
/// </summary>
public enum ObservedConsumerOwner
{
    /// <summary>
    /// Owned by the application.
    /// </summary>
    Application,

    /// <summary>
    /// Owned by the platform, and excluded from comparison.
    /// </summary>
    Platform,

    /// <summary>
    /// Ownership is not known.
    /// </summary>
    Unknown
}

/// <summary>
/// Describes a registered subscription and its optional progress.
/// </summary>
/// <param name="Identifier">The host's consumer identifier.</param>
/// <param name="Kind">The consumer kind.</param>
/// <param name="Owner">The consumer owner.</param>
/// <param name="SequenceId">The subscribed sequence.</param>
/// <param name="EventTypes">The subscribed event generations.</param>
/// <param name="NextSequenceNumber">The next sequence number to handle, if known.</param>
/// <param name="LastHandledSequenceNumber">The last handled sequence number, if known.</param>
public sealed record ObservedConsumer(
    string Identifier,
    ObservedConsumerKind Kind,
    ObservedConsumerOwner Owner,
    string SequenceId,
    ImmutableArray<ObservedEventType> EventTypes,
    long? NextSequenceNumber = null,
    long? LastHandledSequenceNumber = null);
