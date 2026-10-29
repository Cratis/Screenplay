// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies a described command's pending payload implementation and its result contract.
/// </summary>
/// <param name="RequirementId">The stable command-handler requirement identity.</param>
/// <param name="ResultVersion">The result-contract version.</param>
public sealed record SemanticCommandHandler(string RequirementId, uint ResultVersion)
{
    /// <summary>Gets the closed set of event results, empty for the legacy result contract.</summary>
    public ImmutableArray<SemanticProducedEvent> PossibleEvents { get; init; } = [];
}
