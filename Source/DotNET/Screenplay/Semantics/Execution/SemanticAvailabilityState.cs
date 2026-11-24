// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// The facts availability is decided from: which audiences the caller is in and which parts an operator has suspended.
/// </summary>
/// <remarks>
/// Audience membership and suspension are runtime facts that are never in the model. Without a state the reference evaluator reports
/// everything available, which is what specifications run with.
/// </remarks>
/// <param name="Audiences">The names of the audiences the caller is in.</param>
public sealed record SemanticAvailabilityState(ImmutableHashSet<string> Audiences)
{
    /// <summary>
    /// Gets the modules, features, slices, commands and queries an operator has suspended. Suspending a module or a feature suspends everything beneath it.
    /// </summary>
    public ImmutableHashSet<SemanticId> Suspended { get; init; } = [];
}
