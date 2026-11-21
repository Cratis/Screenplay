// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal sealed class SemanticAbilityAnswerValidator(Action<SemanticValue, SemanticTypeReference, string> validateValue)
{
    internal void Validate(SemanticAbilityAnswer answer, SemanticAbility ability, string owner, bool allowPending = false)
    {
        var disallowedPending = !allowPending && answer.Kind == SemanticAbilityAnswerKind.Pending;
        if (!Enum.IsDefined(answer.Kind) || disallowedPending)
            throw new InvalidSemanticContract($"{owner} has an unknown kind.");
        ValidateOutcome(answer, ability, owner);
        if (answer.Reason is not null && answer.Kind != SemanticAbilityAnswerKind.Failed)
            throw new InvalidSemanticContract($"{owner} states a reason without failing.");
        if (answer.Kind != SemanticAbilityAnswerKind.Returned && !answer.Values.IsDefaultOrEmpty)
            throw new InvalidSemanticContract($"{owner} returns values without returning.");
        ValidateValues(answer.Values, ability.Returns, owner);
    }

    internal void ValidateValues(ImmutableArray<SemanticAbilityFieldValue> values, ImmutableArray<SemanticAbilityField> fields, string owner)
    {
        if (values.IsDefault) throw new InvalidSemanticContract($"{owner} requires initialized values.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value.Name) || fields.FirstOrDefault(candidate => candidate.Name == value.Name) is not { } field)
                throw new InvalidSemanticContract($"{owner} states '{value.Name}' more than once or not at all declared.");
            validateValue(value.Value, field.Type, owner);
        }
    }

    static void ValidateOutcome(SemanticAbilityAnswer answer, SemanticAbility ability, string owner)
    {
        var outcomeOnly = answer.Kind == SemanticAbilityAnswerKind.Outcome;
        var declaredOutcome = !outcomeOnly || ability.Outcomes.Contains(answer.Outcome!);
        if (outcomeOnly != (answer.Outcome is not null) || !declaredOutcome)
            throw new InvalidSemanticContract($"{owner} names an outcome only when it answers one, and the ability must declare it.");
    }
}
