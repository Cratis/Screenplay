// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Groups a model comparison by slice, so a consumer that builds slice by slice knows what to rebuild.
/// </summary>
/// <param name="Slices">Every slice that was added, removed, renamed, moved or changed, ordered by address.</param>
/// <param name="ModelWide">Changed declarations no slice owns, such as modules, features, concepts, types, policies and personas.</param>
/// <param name="AffectedSlices">Addresses of unchanged slices that depend on a changed slice or a changed model-wide declaration.</param>
public sealed record SliceChangeSummary(
    IReadOnlyList<SliceChange> Slices,
    IReadOnlyList<ComparedDeclaration> ModelWide,
    IReadOnlyList<string> AffectedSlices)
{
    /// <summary>
    /// Gets a summary without changes.
    /// </summary>
    public static SliceChangeSummary Empty { get; } = new([], [], []);
}
