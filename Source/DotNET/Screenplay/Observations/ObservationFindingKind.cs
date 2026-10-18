// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Identifies a comparison finding; ordinal values define within-declaration report order.
/// </summary>
public enum ObservationFindingKind
{
    /// <summary>
    /// A declared event generation is absent from the whole registry.
    /// </summary>
    EventNotRegistered,

    /// <summary>
    /// A declared generation has no examined appends.
    /// </summary>
    EventNotAppended,

    /// <summary>
    /// An undeclared stored type has examined appends.
    /// </summary>
    UndeclaredEventAppended,

    /// <summary>
    /// An undeclared stored type is registered but not appended.
    /// </summary>
    UndeclaredEventRegistered,

    /// <summary>
    /// Historical appends have no matching registration.
    /// </summary>
    AppendedWithoutRegistration,

    /// <summary>
    /// A known stored type has an undeclared generation.
    /// </summary>
    UndeclaredGeneration,

    /// <summary>
    /// A mapped producer appended an event it does not declare.
    /// </summary>
    UndeclaredEdgeObserved,

    /// <summary>
    /// A declared production has no attributed appends.
    /// </summary>
    DeclaredEdgeNotObserved,

    /// <summary>
    /// No events establish that a producer ran within the scan.
    /// </summary>
    NoMatchingAppendsWithinCoverage,

    /// <summary>
    /// No application reaction consumer subscribes to the trigger event.
    /// </summary>
    ConsumerNotRegistered,

    /// <summary>
    /// A matching application reaction consumer subscribes only on another sequence.
    /// </summary>
    ConsumerOnOtherSequence,

    /// <summary>
    /// A mapped subscription differs, or cannot be compared.
    /// </summary>
    ConsumerMismatch,

    /// <summary>
    /// A foreign event is excluded from local contract comparison.
    /// </summary>
    ForeignEvent,

    /// <summary>
    /// Evidence cannot establish a comparison for a construct or producer.
    /// </summary>
    NotEstablished
}
