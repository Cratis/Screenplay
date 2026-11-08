// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Summarizes how one slice changed between two versions of a model.
/// </summary>
/// <param name="Changes">Every way the slice changed, in <see cref="SliceChangeKind"/> order; never empty.</param>
/// <param name="BeforeAddress">The slice address before the comparison, or <see langword="null"/> when it was added.</param>
/// <param name="AfterAddress">The slice address after the comparison, or <see langword="null"/> when it was removed.</param>
/// <param name="SemanticId">The slice's persisted identity when both models carry identities.</param>
/// <param name="Declarations">The declarations the slice owns that changed, including the slice itself.</param>
/// <param name="DependentSlices">Addresses of other slices that depend on this slice, on the side the slice exists; after the comparison when it exists there.</param>
public sealed record SliceChange(
    IReadOnlyList<SliceChangeKind> Changes,
    string? BeforeAddress,
    string? AfterAddress,
    SemanticId? SemanticId,
    IReadOnlyList<ComparedDeclaration> Declarations,
    IReadOnlyList<string> DependentSlices)
{
    /// <summary>
    /// Gets the most significant change: added, removed, renamed, moved or changed, in that order.
    /// </summary>
    public SliceChangeKind Change => Changes[0];

    /// <summary>
    /// Gets the address that identifies the slice: after the comparison when it still exists, otherwise before.
    /// </summary>
    public string Address => AfterAddress ?? BeforeAddress!;
}
