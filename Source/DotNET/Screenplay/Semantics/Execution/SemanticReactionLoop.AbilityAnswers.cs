// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticReactionLoop
{
    static bool SameCause(SemanticFact expected, SemanticFact actual) =>
        expected with { Destination = actual.Destination, Values = actual.Values, Tags = actual.Tags } == actual &&
        SemanticValueRules.AreEqual(expected.Destination, actual.Destination) && expected.Tags.SequenceEqual(actual.Tags) &&
        SameCauseValues(expected, actual);

    static bool SameCauseValues(SemanticFact expected, SemanticFact actual) =>
        expected.Values.Length == actual.Values.Length && expected.Values.Zip(actual.Values).All(pair =>
            pair.First.TargetProperty == pair.Second.TargetProperty && SemanticValueRules.AreEqual(pair.First.Value, pair.Second.Value));

    void ValidateAbilityAnswer(SemanticInvocation invocation, SemanticAbilityAnswer? answer, bool allowPending = false)
    {
        if (answer is null) return;
        const string owner = "The host's ability answer";
        if (answer.System != invocation.System || answer.Ability != invocation.Ability)
            throw new InvalidSemanticContract($"{owner} does not match the invocation.");
        var system = plan.Model.Application.Systems.FirstOrDefault(candidate => candidate.Id == invocation.System)
            ?? throw new InvalidSemanticContract($"{owner} names an unresolved system.");
        var ability = system.Abilities.FirstOrDefault(candidate => candidate.Name == invocation.Ability)
            ?? throw new InvalidSemanticContract($"{owner} names an ability its system does not declare.");
        new SemanticAbilityAnswerValidator(_validator.Validate).Validate(answer, ability, owner, allowPending);
    }

    void ValidatePendingCause(SemanticPendingAbilityInvocation pending)
    {
        var sequence = pending.Identity.CauseSequence;
        if (sequence < 0 || sequence >= World.Facts.Length)
            throw new InvalidSemanticContract("The pending invocation's cause sequence is not in the world.");
        if (!SameCause(World.Facts[(int)sequence], pending.Cause))
            throw new InvalidSemanticContract("The pending invocation's cause does not match the world.");
    }
}
