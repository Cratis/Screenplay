// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static class SemanticPolicyEvaluation
{
    internal static bool Allows(
        SemanticAuthorization? authorization,
        SemanticExecutionPlan plan,
        SemanticCaller? caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject) => authorization is null ||
        (caller is not null && EvaluateAuthorization(authorization, plan, caller, artifact, subject));

    static bool EvaluateAuthorization(
        SemanticAuthorization authorization,
        SemanticExecutionPlan plan,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject) => authorization switch
    {
        SemanticPolicyReference reference => EvaluateCondition(
            plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, caller, artifact, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical =>
            EvaluateAuthorization(logical.Left, plan, caller, artifact, subject) &&
            EvaluateAuthorization(logical.Right, plan, caller, artifact, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical =>
            EvaluateAuthorization(logical.Left, plan, caller, artifact, subject) ||
            EvaluateAuthorization(logical.Right, plan, caller, artifact, subject),
        _ => throw new InvalidSemanticContract("Unknown authorization node or operator.")
    };

    static bool EvaluateCondition(
        SemanticPolicyCondition condition,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject) => condition switch
    {
        SemanticAuthenticatedCondition => caller.Authenticated,
        SemanticRoleCondition role => caller.Roles.Contains(role.Role, StringComparer.Ordinal),
        SemanticClaimCondition claim => MatchClaim(claim, caller, artifact, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical =>
            EvaluateCondition(logical.Left, caller, artifact, subject) && EvaluateCondition(logical.Right, caller, artifact, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical =>
            EvaluateCondition(logical.Left, caller, artifact, subject) || EvaluateCondition(logical.Right, caller, artifact, subject),
        _ => throw new InvalidSemanticContract("Unknown policy condition node or operator.")
    };

    static bool MatchClaim(SemanticClaimCondition claim, SemanticCaller caller, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject)
    {
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => claim.Value,
            SemanticClaimTargetKind.Subject => Text(subject),
            SemanticClaimTargetKind.Artifact when claim.Value is not null && artifact.TryGetValue(claim.Value, out var value) => Text(value),
            _ => null
        };
        return target is not null && caller.Claims.Any(value =>
            string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(value.Value, target, StringComparison.Ordinal));
    }

    static string? Text(SemanticValue? value) => value is SemanticTextValue text ? text.Value : null;
}
