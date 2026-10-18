// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Defines the strength of a finding, not a behavioral execution verdict.
/// </summary>
public enum ObservationOutcome
{
    /// <summary>
    /// Known disagreement with a declared relationship.
    /// </summary>
    Drift,

    /// <summary>
    /// Bounded evidence that does not establish drift.
    /// </summary>
    Informational,

    /// <summary>
    /// Evidence is insufficient to compare.
    /// </summary>
    Unknown,

    /// <summary>
    /// Outside the local comparison scope.
    /// </summary>
    Excluded
}

/// <summary>
/// Identifies why evidence cannot establish a relationship, or why it is excluded.
/// </summary>
public enum ObservationUnknownReason
{
    /// <summary>
    /// Not all events through the captured tail were examined.
    /// </summary>
    PartialCoverage,

    /// <summary>
    /// Production or trigger has a predicate.
    /// </summary>
    ConditionalProduction,

    /// <summary>
    /// A command has a described handler.
    /// </summary>
    DescribedHandler,

    /// <summary>
    /// A reaction body is opaque.
    /// </summary>
    OpaqueReaction,

    /// <summary>
    /// A reaction trigger is not an event.
    /// </summary>
    NonEventTrigger,

    /// <summary>
    /// More than one construct can match an identity.
    /// </summary>
    AmbiguousIdentity,

    /// <summary>
    /// An observed producer has no model mapping.
    /// </summary>
    UnmappedProducer,

    /// <summary>
    /// A model construct has no host mapping.
    /// </summary>
    UnmappedConstruct,

    /// <summary>
    /// Some appends have no attributable producer.
    /// </summary>
    UnattributedEvents,

    /// <summary>
    /// A platform consumer is excluded.
    /// </summary>
    PlatformConsumer,

    /// <summary>
    /// Consumer progress has not reached the captured tail, or is unknown.
    /// </summary>
    ConsumerBehind,

    /// <summary>
    /// Projection and reducer consumer semantics are not compared.
    /// </summary>
    ConsumerKindNotCompared
}
