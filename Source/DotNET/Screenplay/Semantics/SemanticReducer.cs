// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>The fixed identity of a reducer-built instance.</summary>
public enum SemanticReducerKey
{
    /// <summary>The event source identity of the observed event.</summary>
    EventSourceId = 0
}

/// <summary>The outcome of a reducer transition.</summary>
public enum SemanticReducerResult
{
    /// <summary>A new read-model state, or deletion when the result is null.</summary>
    StateOrDelete = 0
}

/// <summary>One opaque reducer transition attached to an observed event contract.</summary>
/// <param name="EventContract">The observed event declaration identity.</param>
/// <param name="RequirementId">The requirement for this transition's implementation.</param>
public sealed record SemanticReducerTransition(SemanticId EventContract, string RequirementId);

/// <summary>A reducer's portable routing and transition boundary; the body remains opaque.</summary>
/// <param name="Name">The authored reducer name.</param>
/// <param name="ReadModel">The read-model identity built by this reducer.</param>
/// <param name="Transitions">The ordered event-to-implementation requirements.</param>
public sealed record SemanticReducer(string Name, SemanticId ReadModel, ImmutableArray<SemanticReducerTransition> Transitions)
{
    /// <summary>Gets the only supported reducer key: the event source identity.</summary>
    public SemanticReducerKey Key => SemanticReducerKey.EventSourceId;

    /// <summary>Gets the initial state, which is null before the first event.</summary>
    public object? InitialState => null;

    /// <summary>Gets the transition outcome: a new state, or delete on null.</summary>
    public SemanticReducerResult Result => SemanticReducerResult.StateOrDelete;
}
