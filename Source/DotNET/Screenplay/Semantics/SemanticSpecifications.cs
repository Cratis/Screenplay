// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents event values in a semantic specification.
/// </summary>
/// <param name="EventContract">The event declaration semantic identity.</param>
/// <param name="Values">The event property values, canonically ordered by target identity.</param>
public sealed record SemanticSpecificationEvent(
    SemanticId EventContract,
    ImmutableArray<SemanticPropertyValue> Values)
{
    /// <summary>
    /// Gets the exact typed event source asserted for this occurrence; <see langword="null"/> is retained for semantic-v1 compatibility only.
    /// </summary>
    public SemanticEventSourceIdentity? EventSource { get; init; }
}

/// <summary>
/// Represents the event occurrence appended by a semantic specification.
/// </summary>
/// <param name="EventContract">The event contract identity.</param>
/// <param name="Values">The concrete event payload.</param>
public sealed record SemanticSpecificationAppend(SemanticId EventContract, ImmutableArray<SemanticPropertyValue> Values)
{
    /// <summary>Gets the typed source for the occurrence, when explicitly asserted.</summary>
    public SemanticEventSourceIdentity? EventSource { get; init; }
}

/// <summary>
/// Represents the command exercised by a semantic specification.
/// </summary>
/// <param name="Command">The command semantic identity.</param>
/// <param name="Values">The command property values, canonically ordered by target identity.</param>
public sealed record SemanticSpecificationCommand(
    SemanticId Command,
    ImmutableArray<SemanticPropertyValue> Values)
{
    /// <summary>
    /// Gets the exact typed state-change destination asserted for this command occurrence; <see langword="null"/> means no modeled default is applicable under legacy semantic-v1 behavior.
    /// </summary>
    public SemanticEventSourceIdentity? EventSource { get; init; }
}

/// <summary>
/// Represents one keyed read model state in a semantic specification.
/// </summary>
/// <param name="ReadModel">The read model semantic identity.</param>
/// <param name="Key">The runtime instance key.</param>
/// <param name="Values">The read model property values, canonically ordered by target identity.</param>
public sealed record SemanticSpecificationReadModel(
    SemanticId ReadModel,
    SemanticValue Key,
    ImmutableArray<SemanticPropertyValue> Values)
{
    /// <summary>Gets whether actual properties must exactly match the authored properties.</summary>
    public bool Exactly { get; init; }
}

/// <summary>
/// Represents an expected keyed query result.
/// </summary>
/// <param name="Query">The query semantic identity.</param>
/// <param name="Key">The query key.</param>
/// <param name="Results">The expected results in authored comparison order; each read-model/key pair must be unique.</param>
public sealed record SemanticSpecificationQueryResult(
    SemanticId Query,
    SemanticValue Key,
    ImmutableArray<SemanticSpecificationReadModel> Results)
{
    /// <summary>Gets whether every result row must have exactly the authored properties.</summary>
    public bool Exactly { get; init; }
}

/// <summary>
/// Represents an expected validation rejection.
/// </summary>
/// <param name="Code">The stable rejection code, or <see langword="null"/> when only rejection is asserted.</param>
/// <param name="Message">The expected message, or <see langword="null"/> when it is not asserted.</param>
/// <remarks>
/// A value beginning with <c>$strings.</c> is a string-key reference, never display text. The realization
/// resolves it against the active locale's paired <c>.strings</c> file (see internationalization.md).
/// </remarks>
public sealed record SemanticSpecificationError(string? Code, string? Message);

/// <summary>
/// Represents an executable baseline Given/When/Then specification.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="GivenEvents">The events establishing prior state, in occurrence order.</param>
/// <param name="GivenReadModels">The read model states establishing prior state, in authored order with unique read-model/key pairs.</param>
/// <param name="When">The command being exercised, or <see langword="null"/> for a read-only specification.</param>
/// <param name="ThenEvents">The expected events in authored append order.</param>
/// <param name="ThenReadModels">The expected read model states, in authored order with unique read-model/key pairs.</param>
/// <param name="ThenQueries">The expected keyed query results, in authored order with unique query/key pairs.</param>
/// <param name="ThenErrors">The expected validation rejections, in authored order.</param>
public sealed record SemanticSpecification(
    SemanticId Id,
    string Name,
    ImmutableArray<SemanticSpecificationEvent> GivenEvents,
    ImmutableArray<SemanticSpecificationReadModel> GivenReadModels,
    SemanticSpecificationCommand? When,
    ImmutableArray<SemanticSpecificationEvent> ThenEvents,
    ImmutableArray<SemanticSpecificationReadModel> ThenReadModels,
    ImmutableArray<SemanticSpecificationQueryResult> ThenQueries,
    ImmutableArray<SemanticSpecificationError> ThenErrors)
{
    /// <summary>Gets the explicit caller fixture, or null when no identity context was given.</summary>
    public SemanticCaller? GivenCaller { get; init; }

    /// <summary>Gets whether an authorization denial, rather than a validation error, is asserted.</summary>
    public bool ThenDenied { get; init; }

    /// <summary>Gets the appended event action, mutually exclusive with <see cref="When"/>.</summary>
    public SemanticSpecificationAppend? WhenAppended { get; init; }

    /// <summary>Gets whether then-events are compared without regard to occurrence order.</summary>
    public bool ThenEventsInAnyOrder { get; init; }
}
