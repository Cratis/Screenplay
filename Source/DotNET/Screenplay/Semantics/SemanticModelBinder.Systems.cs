// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static SemanticAbilityEffect Effect(string? written) => AbilityEffectClasses.Effective(written) switch
        {
            AbilityEffectClasses.Safe => SemanticAbilityEffect.Safe,
            AbilityEffectClasses.Idempotent => SemanticAbilityEffect.Idempotent,
            _ => SemanticAbilityEffect.Once
        };

        // A model that declares an ability admits its systems (ESM v50, #1250); a model without one binds none, so its bytes and version do not change.
        ImmutableArray<SemanticSystem> BindSystems()
        {
            var systems = syntax.Systems.ToArray();
            if (!systems.Any(system => system.Abilities.Any())) return [];

            var bound = ImmutableArray.CreateBuilder<SemanticSystem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var system in systems)
            {
                if (!seen.Add(system.Name))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"System '{system.Name}' is declared more than once.", system.Location);
                    continue;
                }

                if (system.Description is not null) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"System '{system.Name}' description is authoring metadata.", system.Location);
                var address = SemanticAddress.ForSystem(_applicationIdentity, system.Name);
                var id = SystemId(system);
                bound.Add(new(id, system.Name, [.. system.Abilities.Select(ability => BindAbility(address, system, ability))]));
            }

            return bound.ToImmutable();
        }

        SemanticAbility BindAbility(SemanticAddress system, SystemSyntax owner, AbilitySyntax ability)
        {
            if (ability.Description is not null) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Ability '{owner.Name}.{ability.Name}' description is authoring metadata.", ability.Location);
            foreach (var outcome in ability.Outcomes.Where(outcome => outcome.Description is not null))
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Outcome '{owner.Name}.{ability.Name}.{outcome.Name}' description is authoring metadata.", outcome.Location);
            if (ability.Implementation is { } implementation)
            {
                foreach (var hint in implementation.Hints)
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Ability '{owner.Name}.{ability.Name}' implementation hint is authoring metadata.", hint.Location);
            }

            var requirement = RequireImplementation(SemanticImplementationRole.AbilityEffect, system, ability.File, ability.Code, ability.Name);
            var realization = requirement is null
                ? new SemanticAbilityRealization(SemanticAbilityRealizationKind.None, null)
                : new SemanticAbilityRealization(SemanticAbilityRealizationKind.Code, requirement.RequirementId);
            return new(
                ability.Name,
                Effect(ability.EffectClass),
                [.. ability.Inputs.Select(Field)],
                [.. ability.Returns.Select(Field)],
                [.. ability.Outcomes.Select(outcome => outcome.Name)],
                ability.AnswersLater,
                realization);
        }

        SemanticAbilityField Field(PropertySyntax property) => new(property.Name, BindTypeReference(property.Type));
    }
}
