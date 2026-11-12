// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents one invocation a reaction made during a reference execution, whether or not the command accepted it and
/// whether or not the command is owned by the slice the execution runs.
/// </summary>
/// <param name="Command">The invoked command.</param>
/// <param name="Values">The evaluated, non-generated property values the request carried, in the command's property order.</param>
/// <param name="Accepted">Whether the command accepted the invocation.</param>
public sealed record SemanticRecordedInvocation(SemanticId Command, ImmutableArray<SemanticPropertyValue> Values, bool Accepted)
{
    /// <summary>
    /// Gets the system whose ability was invoked, when the invocation targeted an ability.
    /// </summary>
    public SemanticId System { get; init; }

    /// <summary>
    /// Gets the name of the invoked ability, or <c>null</c> when the invocation targeted a command.
    /// </summary>
    public string? Ability { get; init; }

    /// <summary>
    /// Gets the inputs the ability was invoked with.
    /// </summary>
    public ImmutableArray<SemanticAbilityFieldValue> Inputs { get; init; } = [];
}
