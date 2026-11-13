// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Identifies an invocation deterministically within the complete log prefix used for settlement.
/// </summary>
/// <param name="Reaction">The reaction identity.</param>
/// <param name="CauseSequence">The zero-based world fact index of the cause.</param>
/// <param name="TriggerIndex">The zero-based raw trigger ordinal within the reaction.</param>
/// <param name="Index">The zero-based raw invocation ordinal within the trigger.</param>
public sealed record SemanticAbilityInvocationIdentity(SemanticId Reaction, long CauseSequence, int TriggerIndex, int Index)
{
    /// <summary>
    /// Gets the stable key a host uses to deduplicate effects across retried settlements.
    /// </summary>
    public string Key => string.Create(CultureInfo.InvariantCulture, $"{Reaction}/{CauseSequence}/{TriggerIndex}/{Index}");

    /// <inheritdoc/>
    public override string ToString() => Key;
}
