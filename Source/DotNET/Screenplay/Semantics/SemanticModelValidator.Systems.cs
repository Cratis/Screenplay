// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateSystems(SemanticApplication application, SemanticVersion version)
    {
        if (application.Systems.IsDefaultOrEmpty) return;
        if (!version.IsAtLeast(SemanticVersion.V50)) throw new InvalidSemanticContract("Systems with abilities require ESM v50 or later.");
        if (!application.Systems.Any(system => !system.Abilities.IsDefaultOrEmpty)) throw new InvalidSemanticContract("A model that declares systems must declare at least one ability.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var system in application.Systems)
        {
            if (system?.Id.IsSet != true || string.IsNullOrWhiteSpace(system.Name) || system.Abilities.IsDefault) throw new InvalidSemanticContract("A system requires an identity, a name and an abilities array.");
            if (!names.Add(system.Name)) throw new InvalidSemanticContract($"System '{system.Name}' is declared more than once.");
            var abilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ability in system.Abilities) ValidateAbility(system, ability, abilities);
        }
    }

    static void ValidateAbility(SemanticSystem system, SemanticAbility? ability, HashSet<string> abilities)
    {
        if (ability is null || string.IsNullOrWhiteSpace(ability.Name)) throw new InvalidSemanticContract($"A '{system.Name}' ability requires a name.");
        if (!abilities.Add(ability.Name)) throw new InvalidSemanticContract($"System '{system.Name}' declares ability '{ability.Name}' more than once, ignoring case.");
        if (!Enum.IsDefined(ability.Effect)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' has an unknown effect class.");
        if (ability.Inputs.IsDefault || ability.Returns.IsDefault || ability.Outcomes.IsDefault) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' requires initialized arrays.");
        if (ability.Realization is null || !Enum.IsDefined(ability.Realization.Kind)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' requires a realization.");
        var isCode = ability.Realization.Kind == SemanticAbilityRealizationKind.Code;
        if (isCode == string.IsNullOrEmpty(ability.Realization.RequirementId))
            throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' has a requirement id exactly when its realization is code.");
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in ability.Inputs) if (!fields.Add(field.Name)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' declares input '{field.Name}' more than once.");
        fields.Clear();
        foreach (var field in ability.Returns) if (!fields.Add(field.Name)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' returns '{field.Name}' more than once.");
        var outcomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var outcome in ability.Outcomes)
        {
            if (AbilityOutcomes.ReservedOutlets.Contains(outcome)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' outcome '{outcome}' is a reserved outlet.");
            if (!outcomes.Add(outcome)) throw new InvalidSemanticContract($"Ability '{system.Name}.{ability.Name}' declares outcome '{outcome}' more than once.");
        }
    }
}
