// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// One explicit syntax reference behind an inferred edge.
/// </summary>
/// <param name="Consumer">The referencing slice.</param>
/// <param name="Producer">The earliest matching producer, or imported context.</param>
/// <param name="Kind">The dependency kind.</param>
/// <param name="Role">The reference role, shared with the MCP index.</param>
/// <param name="Name">The referenced declaration name.</param>
/// <param name="Ambiguous">Whether another slice qualifies.</param>
/// <param name="Alternatives">Other qualifying slices, in authored order.</param>
/// <param name="Location">The explicit reference's source location.</param>
internal sealed record DependencyEvidence(DependencyNode Consumer, DependencyNode Producer, string Kind, string Role, string Name, bool Ambiguous, IReadOnlyList<DependencyNode> Alternatives, SourceLocation Location)
{
    /// <summary>
    /// Gets whether this reference occurs only in a specification, including imported facts.
    /// </summary>
    public bool TestOnly { get; init; } = Kind == "verifiedWith";
}
