// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes a declaration excluded because executable binding refused it.
/// </summary>
/// <param name="Kind">The kind of declaration excluded.</param>
/// <param name="Address">The module/feature/slice/specification path, or import or policy name.</param>
/// <param name="ParentId">The enclosing feature or application identity for attaching evidence.</param>
/// <param name="Location">The authored declaration location.</param>
/// <param name="Diagnostics">The original binder errors that required its exclusion.</param>
public sealed record SemanticExclusion(SemanticExclusionKind Kind, string Address, SemanticId? ParentId, SourceLocation Location, ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the security dependency that required this exclusion, when applicable.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets whether the exclusion is a root failure or follows an excluded dependency.
    /// </summary>
    public SemanticExclusionCause Cause { get; init; } = SemanticExclusionCause.Root;

    /// <summary>
    /// Gets the distinct immediate excluded dependencies, ordered by kind and ordinal address.
    /// </summary>
    public ImmutableArray<SemanticExclusionReference> DependsOn { get; init; } = [];
}
