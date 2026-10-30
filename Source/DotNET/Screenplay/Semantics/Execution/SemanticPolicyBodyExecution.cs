// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticPolicyBodyExecution(ISemanticBodyExecutionProvider? provider, SemanticRuleBodyExecution? bodies)
{
    internal static readonly SemanticPolicyBodyExecution None = new(null, null);
    internal bool HasProvider => provider is not null;

    internal static SemanticPolicyBodyExecution For(ISemanticEvaluator evaluator) => evaluator switch
    {
        SemanticEvaluator semantic => semantic.PolicyBodies,
        SemanticCancellationEvaluator cancelled => For(cancelled.Evaluator),
        _ => None
    };

    internal static bool RequiredBy(SemanticExecutionPlan plan, SemanticAuthorization? authorization) => authorization switch
    {
        SemanticPolicyReference reference => plan.Model.Application.Policies.Any(policy => policy.Name == reference.Name && policy.Condition is SemanticOpaquePolicyCondition),
        SemanticLogicalAuthorization logical => RequiredBy(plan, logical.Left) || RequiredBy(plan, logical.Right),
        _ => false
    };

    internal SemanticPolicyDecision Authorize(SemanticPolicy policy, SemanticOpaquePolicyCondition condition, SemanticPolicyContext context, CancellationToken cancellationToken)
    {
        var requirement = condition.RequirementId;
        if (bodies?.FindBody(requirement, SemanticImplementationRole.PolicyPredicate) is not { } body ||
            body.Requirement.Member != policy.Name || body.Requirement.IsAmbiguous || !HasLanguageOrFile(body.Requirement) ||
            body.Requirement.AttachmentResolution != SemanticAttachmentResolution.Resolved ||
            body.Requirement.ContextVersion != 1 || body.Requirement.ResultVersion != 1 ||
            !SemanticRuleBodyExecution.MatchesHash(body))
        {
            return Unsupported(policy, requirement, "requires verified policy source");
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var answer = Task.Run(async () => await provider!.AuthorizeAsync(body, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            return answer switch
            {
                { Outcome: SemanticPolicyBodyOutcome.Allow, Diagnostic: null } => new(SemanticPolicyOutcome.Allow),
                { Outcome: SemanticPolicyBodyOutcome.Deny, Diagnostic: null } => new(SemanticPolicyOutcome.Deny),
                { Outcome: SemanticPolicyBodyOutcome.Unsupported } => Unsupported(policy, requirement, answer.Diagnostic ?? "could not be evaluated"),
                _ => Unsupported(policy, requirement, "returned an invalid policy answer")
            };
        }
        catch (Exception exception)
        {
            return Unsupported(policy, requirement, $"provider failed ({exception.GetType().Name})");
        }
    }

    static bool HasLanguageOrFile(SemanticImplementationRequirement requirement) =>
        requirement.File is not null || !string.IsNullOrWhiteSpace(requirement.Language);

    static SemanticPolicyDecision Unsupported(SemanticPolicy policy, string requirement, string reason) => new(
        SemanticPolicyOutcome.Unsupported,
        policy.Name,
        $"Policy '{policy.Name}' requirement '{requirement}' {SemanticRuleBodyExecution.Sanitize(reason)}.");
}
