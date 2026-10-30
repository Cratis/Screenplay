// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Context version 1 for Snapshot Many performers. The reference run does not construct it until performer
/// queries are admitted to the executable model. It has no causation or Occurred member (decisions 0113 and 0121).
/// </summary>
/// <param name="Arguments">Typed arguments keyed by declared argument property IDs. Hosts map these to the declared names and Arguments properties.</param>
/// <param name="Tenant">The tenant, or null when unavailable.</param>
/// <param name="Identity">The caller identity, or null when unavailable.</param>
/// <param name="ReceivedAt">The request's received time, or null when unavailable.</param>
/// <param name="ReadModels">An immutable snapshot of rows of the declared read model only, never other models or services.</param>
public sealed record SemanticPerformerContext(
    SemanticCompositeValue Arguments,
    string? Tenant,
    SemanticBodyIdentity? Identity,
    DateTimeOffset? ReceivedAt,
    ImmutableArray<SemanticCompositeValue> ReadModels);
