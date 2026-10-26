// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Immutable;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents the result of comparing two normalised semantic models.
/// </summary>
/// <param name="Differences">The differences, sorted by path, ordinal.</param>
internal sealed record SemanticModelComparison(ImmutableArray<SemanticDifference> Differences)
{
    /// <summary>
    /// Gets a value indicating whether the models are semantically equivalent.
    /// </summary>
    public bool IsEquivalent => Differences.IsDefaultOrEmpty;
}
