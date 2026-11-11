// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates every <c>invokes &lt;System&gt;.&lt;Ability&gt;</c> call site: reactions, workflow actions and approvals,
/// state machine entries and operation phases.
/// </summary>
internal static class InvocationValidator
{
    [ThreadStatic]
    static Dictionary<string, string>? _concepts;

    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        _concepts = application.Concepts.GroupBy(concept => concept.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First().Type, StringComparer.Ordinal);
        var abilities = new Dictionary<string, AbilitySyntax>(StringComparer.Ordinal);
        foreach (var system in application.Systems)
        {
            foreach (var ability in system.Abilities) abilities.TryAdd($"{system.Name}.{ability.Name}", ability);
        }

        foreach (var (slice, _) in declarations.Slices)
        {
            foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers)) ValidateTrigger(trigger, abilities, context);
            foreach (var phase in OperationDeclarations.In(slice).SelectMany(operation => new[] { operation.Execute, operation.Compensate }).OfType<OperationPhaseSyntax>())
            {
                foreach (var invocation in phase.Invocations) ValidateSite(invocation.Command, invocation.Mappings, invocation.Location, abilities, context, out _);
            }
        }

        foreach (var (slice, _) in declarations.Slices)
        {
            foreach (var answer in slice.Specifications.SelectMany(specification => specification.GivenAbilityAnswers)) ValidateAnswer(answer, abilities, context);
        }

        foreach (var machine in application.StateMachines)
        {
            foreach (var entry in machine.States.SelectMany(state => state.Entering).Where(entry => entry.EntryKind == StateEntryKind.Invoke))
            {
                if (!ValidateSite(entry.Target, entry.Mappings, entry.Location, abilities, context, out var ability)) continue;
                if (ability.AnswersLater)
                    context.Error(DiagnosticCodes.AnswersLaterInStateMachine, $"'{entry.Target}' answers later; a state machine has no data to hold the answer - start a workflow instead.", entry.Location);
            }
        }

        foreach (var workflow in application.Workflows)
        {
            ValidateWorkflow(workflow, abilities, context);
            WorkflowDominance.Validate(workflow, context);
        }

        new InvocationValueScopeWalker(context).VisitApplication(application);
    }

    static void ValidateAnswer(SpecificationAbilityAnswerSyntax answer, Dictionary<string, AbilitySyntax> abilities, ParserContext context)
    {
        if (!abilities.TryGetValue(answer.Ability, out var ability))
        {
            context.Error(DiagnosticCodes.UnknownInvocationTarget, $"'{answer.Ability}' is not a declared system ability.", answer.Location);
            return;
        }

        if (answer.Answer == "answers" && !ability.Outcomes.Any(outcome => outcome.Name == answer.Outcome))
            context.Error(DiagnosticCodes.UnknownInvocationOutcome, $"'{answer.Ability}' declares no outcome '{answer.Outcome}'.", answer.Location);
        foreach (var value in answer.Values.Where(value => !ability.Returns.Any(field => field.Name == value.Property)))
            context.Error(DiagnosticCodes.InvalidAbilityAnswer, $"'{answer.Ability}' returns no '{value.Property}'.", value.Location);
    }

    static void ValidateTrigger(ReactionTriggerSyntax trigger, Dictionary<string, AbilitySyntax> abilities, ParserContext context)
    {
        foreach (var invocation in trigger.Invokes ?? [])
        {
            var dotted = invocation.Command.Contains('.', StringComparison.Ordinal);
            AbilitySyntax? ability = null;
            if (dotted)
            {
                if (!ValidateSite(invocation.Command, invocation.Mappings, invocation.Location, abilities, context, out ability)) continue;
                if (ability.AnswersLater)
                    context.Error(DiagnosticCodes.AnswersLaterInReaction, $"'{invocation.Command}' answers later; a reaction has no place to wait - invoke it from a workflow.", invocation.Location);
                if (trigger.Code is not null || trigger.File is not null)
                    context.Error(DiagnosticCodes.InvocationWithAttachedBody, $"A trigger that invokes '{invocation.Command}' cannot also carry an attached body.", invocation.Location);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in invocation.Branches)
            {
                if (!seen.Add(branch.Selector))
                    context.Error(DiagnosticCodes.DuplicateInvocationBranch, $"'{invocation.Command}' handles 'on {branch.Selector}' more than once.", branch.Location);
                else if (InvocationBranchScope.IsOutcome(branch.Selector) && ability?.Outcomes.Any(outcome => outcome.Name == branch.Selector) != true)
                    context.Error(DiagnosticCodes.UnknownInvocationOutcome, $"'{invocation.Command}' declares no outcome '{branch.Selector}'.", branch.Location);
            }
        }
    }

    static void ValidateWorkflow(WorkflowSyntax workflow, Dictionary<string, AbilitySyntax> abilities, ParserContext context)
    {
        foreach (var node in workflow.Nodes)
        {
            switch (node.NodeKind)
            {
                case WorkflowNodeKind.Action when node.Command?.Contains('.', StringComparison.Ordinal) == true:
                    ValidateAction(node, abilities, context);
                    break;
                case WorkflowNodeKind.Approval when node.Via is not null:
                    ValidateApproval(node, abilities, context);
                    break;
                case WorkflowNodeKind.Compute:
                    ValidateCompute(node, context);
                    break;
            }
        }
    }

    static void ValidateAction(WorkflowNodeSyntax node, Dictionary<string, AbilitySyntax> abilities, ParserContext context)
    {
        if (!ValidateSite(node.Command!, node.Mappings, node.Location, abilities, context, out var ability)) return;
        if (node.Attempts > 1 && AbilityEffectClasses.Effective(ability.EffectClass) == AbilityEffectClasses.Once)
            context.Warning(DiagnosticCodes.OnceAbilityRetried, $"'{node.Command}' is a 'once' ability invoked with {node.Attempts} attempts; a repeated attempt can repeat its effect.", node.Location);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in node.Edges)
        {
            if (!seen.Add(edge.Outlet))
            {
                continue;
            }

            if (edge.Outlet == "expired")
            {
                if (!ability.AnswersLater || node.ExpiresAfter is null)
                    context.Error(DiagnosticCodes.UnreachableInvocationOutlet, $"Outlet 'expired' of '{node.Name}' is never taken: '{node.Command}' must answer later and the action must say 'expires after'.", edge.Location);
            }
            else if (InvocationBranchScope.IsOutcome(edge.Outlet) && !InvocationBranchScope.Is(edge.Outlet, "next") && !ability.Outcomes.Any(outcome => outcome.Name == edge.Outlet))
            {
                context.Error(DiagnosticCodes.UnknownInvocationOutcome, $"'{node.Command}' declares no outcome '{edge.Outlet}'.", edge.Location);
            }
        }
    }

    static void ValidateApproval(WorkflowNodeSyntax node, Dictionary<string, AbilitySyntax> abilities, ParserContext context)
    {
        if (!abilities.TryGetValue(node.Via!, out var ability))
        {
            context.Error(DiagnosticCodes.UnknownInvocationTarget, $"Approval '{node.Name}' is asked via '{node.Via}', which is not a declared ability.", node.Location);
            return;
        }

        var outcomes = ability.Outcomes.Select(outcome => outcome.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!ability.AnswersLater || !outcomes.Contains("approved") || !outcomes.Contains("rejected"))
            context.Error(DiagnosticCodes.ApprovalViaAbilityLacksOutcomes, $"'{node.Via}' must answer later with the outcomes 'approved' and 'rejected' to be used for approval '{node.Name}'.", node.Location);
    }

    static void ValidateCompute(WorkflowNodeSyntax node, ParserContext context)
    {
        if (!node.Returns.Any())
            context.Error(DiagnosticCodes.InvalidComputeDeclaration, $"Compute '{node.Name}' declares no 'returns'.", node.Location);
        if (node.Code is null && node.File is null)
            context.Error(DiagnosticCodes.InvalidComputeDeclaration, $"Compute '{node.Name}' has no body: add a tagged fence or a file.", node.Location);
        var outlets = node.Edges.Select(edge => edge.Outlet).Distinct(StringComparer.Ordinal).ToList();
        var single = outlets.Count == 1 && outlets[0] == "next";
        var named = outlets.Count >= 2 && outlets.TrueForAll(outlet => !AbilityOutcomes.ReservedOutlets.Contains(outlet));
        if (!single && !named)
            context.Error(DiagnosticCodes.InvalidComputeDeclaration, $"Compute '{node.Name}' has either one 'next' outlet or two or more author-named outlets; reserved outlet names are not allowed.", node.Location);
    }

    static bool ValidateSite(
        string target,
        IEnumerable<PropertyMappingSyntax> mappings,
        SourceLocation location,
        Dictionary<string, AbilitySyntax> abilities,
        ParserContext context,
        [NotNullWhen(true)] out AbilitySyntax? ability)
    {
        if (!abilities.TryGetValue(target, out ability))
        {
            if (target.Contains('.', StringComparison.Ordinal))
                context.Error(DiagnosticCodes.UnknownInvocationTarget, $"'{target}' is not a declared system ability.", location);
            return false;
        }

        var supplied = new HashSet<string>(StringComparer.Ordinal);
        var inputs = ability.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (!inputs.TryGetValue(mapping.Property, out var input))
            {
                context.Error(DiagnosticCodes.UnknownAbilityInput, $"'{target}' declares no input '{mapping.Property}'.", mapping.Location);
            }
            else if (!supplied.Add(mapping.Property))
            {
                context.Error(DiagnosticCodes.UnknownAbilityInput, $"Input '{mapping.Property}' of '{target}' is mapped more than once.", mapping.Location);
            }
            else if (!LiteralFits(mapping.Source, input.Type))
            {
                context.Error(DiagnosticCodes.IncompatibleAbilityInputMapping, $"The value mapped to '{mapping.Property}' cannot be a {input.Type.Name}.", mapping.Location);
            }
        }

        foreach (var input in inputs.Values.Where(input => !input.Type.IsOptional && !supplied.Contains(input.Name)))
            context.Error(DiagnosticCodes.MissingAbilityInput, $"'{target}' needs input '{input.Name}'.", location);
        return true;
    }

    static bool LiteralFits(ExpressionSyntax source, TypeRefSyntax type)
    {
        if (source is not LiteralExpressionSyntax { Value: { } value } || type.IsCollection) return true;
        return ((_concepts ?? []).GetValueOrDefault(type.Name) ?? type.Name) switch
        {
            "String" or "Uuid" or "Date" or "DateTime" => value is string,
            "Bool" => value is bool,
            "Int" or "Decimal" => value is not (string or bool),
            _ => true
        };
    }
}
