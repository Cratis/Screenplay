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
    ImmutableArray<SemanticPropertyValue> Values);

/// <summary>
/// Represents the command exercised by a semantic specification.
/// </summary>
/// <param name="Command">The command semantic identity.</param>
/// <param name="Values">The command property values, canonically ordered by target identity.</param>
public sealed record SemanticSpecificationCommand(
    SemanticId Command,
    ImmutableArray<SemanticPropertyValue> Values);

/// <summary>
/// Represents one keyed read model state in a semantic specification.
/// </summary>
/// <param name="ReadModel">The read model semantic identity.</param>
/// <param name="Key">The runtime instance key.</param>
/// <param name="Values">The read model property values, canonically ordered by target identity.</param>
public sealed record SemanticSpecificationReadModel(
    SemanticId ReadModel,
    SemanticValue Key,
    ImmutableArray<SemanticPropertyValue> Values);

/// <summary>
/// Represents an expected keyed query result.
/// </summary>
/// <param name="Query">The query semantic identity.</param>
/// <param name="Key">The query key.</param>
/// <param name="Results">The expected results in deterministic order.</param>
public sealed record SemanticSpecificationQueryResult(
    SemanticId Query,
    SemanticValue Key,
    ImmutableArray<SemanticSpecificationReadModel> Results);

/// <summary>
/// Represents an expected validation rejection.
/// </summary>
/// <param name="Code">The stable rejection code, or <see langword="null"/> when only rejection is asserted.</param>
/// <param name="Message">The expected message, or <see langword="null"/> when it is not asserted.</param>
public sealed record SemanticSpecificationError(string? Code, string? Message);

/// <summary>
/// Represents an executable baseline Given/When/Then specification.
/// </summary>
/// <param name="Id">The stable semantic identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="GivenEvents">The events establishing prior state, in occurrence order.</param>
/// <param name="GivenReadModels">The read model states establishing prior state.</param>
/// <param name="When">The command being exercised.</param>
/// <param name="ThenEvents">The expected events in append order.</param>
/// <param name="ThenReadModels">The expected read model states.</param>
/// <param name="ThenQueries">The expected keyed query results.</param>
/// <param name="ThenErrors">The expected validation rejections.</param>
public sealed record SemanticSpecification(
    SemanticId Id,
    string Name,
    ImmutableArray<SemanticSpecificationEvent> GivenEvents,
    ImmutableArray<SemanticSpecificationReadModel> GivenReadModels,
    SemanticSpecificationCommand When,
    ImmutableArray<SemanticSpecificationEvent> ThenEvents,
    ImmutableArray<SemanticSpecificationReadModel> ThenReadModels,
    ImmutableArray<SemanticSpecificationQueryResult> ThenQueries,
    ImmutableArray<SemanticSpecificationError> ThenErrors);
