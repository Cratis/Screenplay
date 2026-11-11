// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesAbilityInvocations(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers)
            .SelectMany(trigger => trigger.Invokes).Any(invocation => invocation?.Ability is not null);

    private sealed partial class ValidationContext
    {
        string? _invocationBranch;
        SemanticAbility? _invocationAbility;

        void ValidateAbilityInvocation(
            SemanticReaction reaction,
            SemanticReactionTrigger trigger,
            SemanticInvocation invocation,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> values)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V51)) throw new InvalidSemanticContract("Ability invocations require ESM v51 or later.");
            if (invocation.Command.IsSet || !invocation.Mappings.IsDefaultOrEmpty || !invocation.OnRefused.IsDefaultOrEmpty)
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an ability, which carries no command, command mappings or refusal branches.");
            RequireObjects(invocation.Inputs, nameof(invocation.Inputs), "ability input");
            RequireObjects(invocation.Branches, nameof(invocation.Branches), "invocation branch");
            var system = _eventRoutesApplication.Systems.FirstOrDefault(candidate => candidate.Id == invocation.System)
                ?? throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an unresolved system.");
            var ability = system.Abilities.FirstOrDefault(candidate => candidate.Name == invocation.Ability)
                ?? throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an unresolved ability.");
            var declared = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var input in invocation.Inputs)
            {
                if (!declared.TryGetValue(input.Input, out var field)) throw new InvalidSemanticContract($"Reaction '{reaction.Name}' gives '{system.Name}.{ability.Name}' an input it does not declare.");
                if (!mapped.Add(input.Input)) throw new InvalidSemanticContract($"Reaction '{reaction.Name}' gives '{system.Name}.{ability.Name}' input '{input.Input}' more than once.");
                if (input.Source is SemanticValueExpression { Kind: SemanticExpressionKind.Value } value)
                {
                    ValidateValue(value.Value, field.Type, "ability input", false);
                    continue;
                }

                RequireCompatible(ResolveExpression(input.Source, root, values), field.Type, "ability input");
            }

            if (ability.Inputs.Any(input => !input.Type.IsOptional && !mapped.Contains(input.Name)))
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must give '{system.Name}.{ability.Name}' every required input.");
            var selectors = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in invocation.Branches)
            {
                if (!selectors.Add(branch.Selector) || (InvocationBranchScope.IsOutcome(branch.Selector) && !ability.Outcomes.Contains(branch.Selector)))
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' handles '{branch.Selector}' of '{system.Name}.{ability.Name}' more than once or not at all declared.");
                RequireObjects(branch.Produces, nameof(branch.Produces), "produced event");
                _invocationBranch = branch.Selector;
                _invocationAbility = ability;
                try
                {
                    foreach (var produced in branch.Produces) ValidateReactionProduces(reaction, trigger, produced, root, values);
                }
                finally
                {
                    _invocationBranch = null;
                    _invocationAbility = null;
                }
            }
        }

        SemanticTypeReference ResolveInvocationValue(SemanticInvocationValueExpression value)
        {
            ValidateTypeReference(value.Type);
            var type = value.Source switch
            {
                "outcome" when value.Member.Length == 0 && InvocationBranchScope.IsOutcome(_invocationBranch) => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
                "failure" when InvocationBranchScope.IsFailureMember(value.Member) && InvocationBranchScope.Is(_invocationBranch, "failed") => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
                "result" when InvocationBranchScope.Is(_invocationBranch, "returned") => _invocationAbility?.Returns.FirstOrDefault(field => field.Name == value.Member)?.Type,
                _ => null
            };
            if (type is null || type != value.Type) throw new InvalidSemanticContract($"'${value.Source}' is not available here or has a type its source does not declare.");
            return type;
        }
    }
}
