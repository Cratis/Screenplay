// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        SemanticAbility? ValidateLifecycleAbilityCall(
            string where,
            SemanticId system,
            string? abilityName,
            ImmutableArray<SemanticAbilityInputMapping> inputs,
            Dictionary<SemanticId, SemanticProperty> subject,
            Dictionary<string, ImmutableArray<SemanticAbilityField>>? nodeResults)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V51)) throw new InvalidSemanticContract($"Ability invocations require ESM v51 or later ({where}).");
            RequireObjects(inputs, nameof(inputs), "ability input");
            var owner = _eventRoutesApplication.Systems.FirstOrDefault(candidate => candidate.Id == system)
                ?? throw new InvalidSemanticContract($"The {where} invokes an unresolved system.");
            var ability = owner.Abilities.FirstOrDefault(candidate => candidate.Name == abilityName)
                ?? throw new InvalidSemanticContract($"The {where} invokes an unresolved ability.");
            var declared = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            var byName = subject.Values.ToDictionary(property => property.Name, StringComparer.Ordinal);
            foreach (var input in inputs)
            {
                if (!declared.TryGetValue(input.Input, out var field)) throw new InvalidSemanticContract($"The {where} gives '{owner.Name}.{ability.Name}' an input it does not declare.");
                if (!mapped.Add(input.Input)) throw new InvalidSemanticContract($"The {where} gives '{owner.Name}.{ability.Name}' input '{input.Input}' more than once.");
                switch (input.Source)
                {
                    case SemanticValueExpression { Kind: SemanticExpressionKind.Value } value:
                        ValidateValue(value.Value, field.Type, "ability input", false);
                        break;
                    case SemanticInvocationValueExpression read:
                        ValidateTypeReference(read.Type);
                        var type = read.Source switch
                        {
                            "subject" => byName.GetValueOrDefault(read.Member)?.Type,
                            "node" when read.Member.IndexOf('.', StringComparison.Ordinal) is > 0 and var dot && nodeResults is not null &&
                                nodeResults.TryGetValue(read.Member[..dot], out var returned) => returned.FirstOrDefault(result => result.Name == read.Member[(dot + 1)..])?.Type,
                            _ => null
                        };
                        if (type is null || type != read.Type) throw new InvalidSemanticContract($"The {where} reads '{read.Source}.{read.Member}', which is not available or has a type its source does not declare.");
                        RequireCompatible(type, field.Type, "ability input");
                        break;
                    default:
                        throw new InvalidSemanticContract($"The {where} maps an ability input from an expression a lifecycle cannot read.");
                }
            }

            if (ability.Inputs.Any(input => !input.Type.IsOptional && !mapped.Contains(input.Name)))
                throw new InvalidSemanticContract($"The {where} must give '{owner.Name}.{ability.Name}' every required input.");
            return ability;
        }

        Dictionary<string, ImmutableArray<SemanticAbilityField>> LifecycleNodeResults(SemanticWorkflow workflow)
        {
            var results = new Dictionary<string, ImmutableArray<SemanticAbilityField>>(StringComparer.Ordinal);
            foreach (var node in workflow.Nodes)
            {
                if (node.Kind == SemanticWorkflowNodeKind.Compute)
                {
                    results[node.Name] = node.Returns.IsDefault ? [] : node.Returns;
                }
                else if (node.Ability is not null && _eventRoutesApplication.Systems.FirstOrDefault(system => system.Id == node.System)?.Abilities.FirstOrDefault(ability => ability.Name == node.Ability) is { } found)
                {
                    results[node.Name] = found.Returns;
                }
            }

            return results;
        }
    }
}
