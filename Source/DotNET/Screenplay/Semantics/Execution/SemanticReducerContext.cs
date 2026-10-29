// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies the catalog-v1 context of one reducer transition.
/// </summary>
public sealed record SemanticReducerContext
{
    /// <summary>Gets the prior read-model composite, or null before an instance exists.</summary>
    public SemanticValue? State { get; init; }

    /// <summary>Gets the event payload composite.</summary>
    public required SemanticValue Event { get; init; }

    /// <summary>Gets the event source identity as catalog Text, using invariant scalar formatting.</summary>
    public required SemanticValue Key { get; init; }

    /// <summary>Gets the tenant, or null when unknown to the reference world.</summary>
    public string? Tenant { get; init; }

    /// <summary>Gets the supplied fact occurrence time, or null when unknown.</summary>
    public DateTimeOffset? Occurred { get; init; }

    /// <summary>
    /// Gets the zero-based position in this world's history, not a global log position.
    /// Bodies depending on absolute log positions are not portable.
    /// </summary>
    public ulong SequenceNumber { get; init; }

    /// <summary>Gets whether this transition has no prior state.</summary>
    public bool IsFirst => State is null;
}
