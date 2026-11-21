// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Carries a pending invocation and its evaluation context for a later answer, without retaining loop state.
/// </summary>
/// <param name="Identity">The stable invocation identity.</param>
/// <param name="TriggerIndex">The raw trigger ordinal, counting every trigger regardless of its condition.</param>
/// <param name="InvocationIndex">The raw invocation ordinal within that trigger.</param>
/// <param name="System">The declaring system.</param>
/// <param name="Ability">The ability name.</param>
/// <param name="Inputs">The mapped input values.</param>
/// <param name="Cause">The fact that caused the invocation.</param>
/// <param name="Occurred">The occurrence time supplied for the cause.</param>
public sealed record SemanticPendingAbilityInvocation(
    SemanticAbilityInvocationIdentity Identity,
    int TriggerIndex,
    int InvocationIndex,
    SemanticId System,
    string Ability,
    ImmutableArray<SemanticAbilityFieldValue> Inputs,
    SemanticFact Cause,
    DateTimeOffset? Occurred)
{
    /// <summary>
    /// Gets the mapped inputs, treating a default array as empty.
    /// </summary>
    public ImmutableArray<SemanticAbilityFieldValue> Inputs { get; init; } = Inputs.IsDefault ? [] : Inputs;
}
