// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesAbilityInvocations(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers)
            .SelectMany(trigger => trigger.Invokes).Any(invocation => invocation?.Ability is not null) ||
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).SelectMany(slice => slice.Specifications)
            .Any(specification => !specification.GivenAbilityAnswers.IsDefaultOrEmpty || specification.ThenInvocations.Any(invocation => invocation?.Ability is not null)) ||
        (!application.Workflows.IsDefaultOrEmpty && application.Workflows.SelectMany(workflow => workflow.Nodes)
            .Any(node => node.Kind == SemanticWorkflowNodeKind.Compute || node.Ability is not null)) ||
        (!application.StateMachines.IsDefaultOrEmpty && application.StateMachines.SelectMany(machine => machine.States).SelectMany(state => state.Entering)
            .Any(entry => entry.Kind is SemanticStateEntryKind.InvokeAbility or SemanticStateEntryKind.RunBody));

    private sealed partial class ValidationContext
    {
        string? _invocationBranch;
        SemanticAbility? _invocationAbility;

        SemanticAbilityAnswerValidator AbilityAnswerValidator => new((value, type, owner) => ValidateValue(value, type, owner, false));

        void ValidateAbilityInvocation(
            SemanticReaction reaction,
            SemanticReactionTrigger trigger,
            SemanticInvocation invocation,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> values)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V51)) throw new InvalidSemanticContract("Ability invocations require ESM v51 or later.");
            if (CarriesCommandInvocationData(invocation))
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an ability, which carries no command, command mappings or refusal branches.");
            RequireObjects(invocation.Inputs, nameof(invocation.Inputs), "ability input");
            RequireObjects(invocation.Branches, nameof(invocation.Branches), "invocation branch");
            var system = _eventRoutesApplication.Systems.FirstOrDefault(candidate => candidate.Id == invocation.System)
                ?? throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an unresolved system.");
            var ability = system.Abilities.FirstOrDefault(candidate => candidate.Name == invocation.Ability)
                ?? throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an unresolved ability.");
            var scope = new AbilityInvocationScope(reaction, trigger, root, values);
            ValidateAbilityInputs(scope, invocation, system, ability);
            ValidateAbilityBranches(scope, invocation, system, ability);
        }

        static bool CarriesCommandInvocationData(SemanticInvocation invocation) =>
            invocation.Command.IsSet || !invocation.Mappings.IsDefaultOrEmpty || !invocation.OnRefused.IsDefaultOrEmpty;

        void ValidateAbilityInputs(
            AbilityInvocationScope scope,
            SemanticInvocation invocation,
            SemanticSystem system,
            SemanticAbility ability)
        {
            var declared = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var input in invocation.Inputs)
            {
                if (!declared.TryGetValue(input.Input, out var field)) throw new InvalidSemanticContract($"Reaction '{scope.Reaction.Name}' gives '{system.Name}.{ability.Name}' an input it does not declare.");
                if (!mapped.Add(input.Input)) throw new InvalidSemanticContract($"Reaction '{scope.Reaction.Name}' gives '{system.Name}.{ability.Name}' input '{input.Input}' more than once.");
                if (input.Source is SemanticValueExpression { Kind: SemanticExpressionKind.Value } value)
                {
                    ValidateValue(value.Value, field.Type, "ability input", false);
                    continue;
                }

                RequireCompatible(ResolveExpression(input.Source, scope.Root, scope.Values), field.Type, "ability input");
            }

            if (ability.Inputs.Any(input => !input.Type.IsOptional && !mapped.Contains(input.Name)))
                throw new InvalidSemanticContract($"Reaction '{scope.Reaction.Name}' must give '{system.Name}.{ability.Name}' every required input.");
        }

        void ValidateAbilityBranches(
            AbilityInvocationScope scope,
            SemanticInvocation invocation,
            SemanticSystem system,
            SemanticAbility ability)
        {
            var selectors = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in invocation.Branches)
            {
                if (!selectors.Add(branch.Selector) || NamesUndeclaredOutcome(branch, ability))
                    throw new InvalidSemanticContract($"Reaction '{scope.Reaction.Name}' handles '{branch.Selector}' of '{system.Name}.{ability.Name}' more than once or not at all declared.");
                RequireObjects(branch.Produces, nameof(branch.Produces), "produced event");
                _invocationBranch = branch.Selector;
                _invocationAbility = ability;
                try
                {
                    foreach (var produced in branch.Produces) ValidateReactionProduces(scope.Reaction, scope.Trigger, produced, scope.Root, scope.Values);
                }
                finally
                {
                    _invocationBranch = null;
                    _invocationAbility = null;
                }
            }
        }

        static bool NamesUndeclaredOutcome(SemanticInvocationBranch branch, SemanticAbility ability) =>
            InvocationBranchScope.IsOutcome(branch.Selector) && !ability.Outcomes.Contains(branch.Selector);

        SemanticAbility ResolveAbility(SemanticId system, string? name, string owner)
        {
            var declared = _eventRoutesApplication.Systems.FirstOrDefault(candidate => candidate.Id == system)
                ?? throw new InvalidSemanticContract($"{owner} names an unresolved system.");
            return declared.Abilities.FirstOrDefault(candidate => candidate.Name == name)
                ?? throw new InvalidSemanticContract($"{owner} names an ability its system does not declare.");
        }

        void ValidateAbilityValues(ImmutableArray<SemanticAbilityFieldValue> values, ImmutableArray<SemanticAbilityField> fields, string owner) =>
            AbilityAnswerValidator.ValidateValues(values, fields, owner);

        void ValidateAbilityInvocationStep(SemanticSpecification specification, SemanticSpecificationInvocation invocation)
        {
            var owner = $"Specification '{specification.Name}' states an ability invocation that";
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V51)) throw new InvalidSemanticContract("Ability invocations require ESM v51 or later.");
            if (invocation.Command.IsSet || !invocation.Values.IsDefaultOrEmpty) throw new InvalidSemanticContract($"{owner} also names a command.");
            ValidateAbilityValues(invocation.Inputs, ResolveAbility(invocation.System, invocation.Ability, owner).Inputs, owner);
        }

        void ValidateAbilityAnswers(SemanticSpecification specification)
        {
            RequireObjects(specification.GivenAbilityAnswers, nameof(specification.GivenAbilityAnswers), "ability answer");
            if (specification.GivenAbilityAnswers.IsDefaultOrEmpty) return;
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V51)) throw new InvalidSemanticContract("Ability answers require ESM v51 or later.");
            var seen = new HashSet<(SemanticId, string)>();
            foreach (var answer in specification.GivenAbilityAnswers)
            {
                var owner = $"Specification '{specification.Name}' gives an ability answer that";
                var ability = ResolveAbility(answer.System, answer.Ability, owner);
                if (!seen.Add((answer.System, answer.Ability))) throw new InvalidSemanticContract($"{owner} repeats an ability.");
                AbilityAnswerValidator.Validate(answer, ability, owner);
            }
        }

        SemanticTypeReference ResolveInvocationValue(SemanticInvocationValueExpression value)
        {
            ValidateTypeReference(value.Type);
            var type = value.Source switch
            {
                "outcome" => ResolveOutcomeValue(value.Member),
                "failure" => ResolveFailureValue(value.Member),
                "result" => ResolveReturnedValue(value.Member),
                _ => null
            };
            if (type is null || type != value.Type) throw new InvalidSemanticContract($"'${value.Source}' is not available here or has a type its source does not declare.");
            return type;
        }

        SemanticTypeReference? ResolveOutcomeValue(string member) =>
            member.Length == 0 && InvocationBranchScope.IsOutcome(_invocationBranch)
                ? SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
                : null;

        SemanticTypeReference? ResolveFailureValue(string member) =>
            InvocationBranchScope.IsFailureMember(member) && InvocationBranchScope.Is(_invocationBranch, "failed")
                ? SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
                : null;

        SemanticTypeReference? ResolveReturnedValue(string member) =>
            InvocationBranchScope.Is(_invocationBranch, "returned")
                ? _invocationAbility?.Returns.FirstOrDefault(field => field.Name == member)?.Type
                : null;

        sealed record AbilityInvocationScope(
            SemanticReaction Reaction,
            SemanticReactionTrigger Trigger,
            SemanticExpressionRootKind Root,
            Dictionary<SemanticId, SemanticProperty> Values);
    }
}
