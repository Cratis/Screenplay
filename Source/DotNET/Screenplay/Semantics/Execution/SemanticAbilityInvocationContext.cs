// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Carries an evaluated ability invocation to a live host.
/// </summary>
/// <param name="System">The declaring system.</param>
/// <param name="Ability">The ability name.</param>
/// <param name="Inputs">The mapped input values.</param>
/// <param name="Reaction">The reaction identity.</param>
/// <param name="Cause">The fact that caused this invocation.</param>
/// <param name="Occurred">The occurrence time supplied for the cause.</param>
/// <param name="Identity">The stable invocation identity.</param>
public sealed record SemanticAbilityInvocationContext(
    SemanticId System,
    string Ability,
    ImmutableArray<SemanticAbilityFieldValue> Inputs,
    SemanticId Reaction,
    SemanticFact Cause,
    DateTimeOffset? Occurred,
    SemanticAbilityInvocationIdentity Identity)
{
    /// <summary>
    /// Gets the mapped inputs, treating a default array as empty.
    /// </summary>
    public ImmutableArray<SemanticAbilityFieldValue> Inputs { get; init; } = Inputs.IsDefault ? [] : Inputs;
}
