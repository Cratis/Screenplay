// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal enum SemanticPolicyOutcome
{
    Allow,
    Deny,
    Unsupported
}

internal readonly record struct SemanticPolicyDecision(SemanticPolicyOutcome Outcome, string? Policy = null);

internal static class SemanticPolicyEvaluation
{
    internal static SemanticPolicyDecision Evaluate(
        SemanticAuthorization? authorization,
        SemanticExecutionPlan plan,
        SemanticCaller? caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties)
    {
        if (authorization is null) return new(SemanticPolicyOutcome.Allow);
        if (caller is not { Roles.IsDefault: false, Claims.IsDefault: false }) return new(SemanticPolicyOutcome.Deny);

        return EvaluateAuthorization(authorization, plan, caller, artifact, subject, properties);
    }

    static SemanticPolicyDecision EvaluateAuthorization(
        SemanticAuthorization authorization,
        SemanticExecutionPlan plan,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties)
    {
        if (authorization is SemanticPolicyReference reference)
        {
            var condition = plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name).Condition;
            return condition is SemanticOpaquePolicyCondition
                ? new(SemanticPolicyOutcome.Unsupported, reference.Name)
                : new(EvaluateCondition(condition, plan, caller, artifact, subject, properties)
                    ? SemanticPolicyOutcome.Allow : SemanticPolicyOutcome.Deny);
        }

        if (authorization is not SemanticLogicalAuthorization logical ||
            logical.Operator is not (SemanticLogicalOperator.And or SemanticLogicalOperator.Or))
        {
            throw new InvalidSemanticContract("Unknown authorization node or operator.");
        }

        var left = EvaluateAuthorization(logical.Left, plan, caller, artifact, subject, properties);

        // A definitive left operand preserves the authored short circuit. Otherwise evaluate the
        // right operand: false AND unknown is false; true OR unknown is true, regardless of order.
        if ((logical.Operator == SemanticLogicalOperator.And && left.Outcome == SemanticPolicyOutcome.Deny) ||
            (logical.Operator == SemanticLogicalOperator.Or && left.Outcome == SemanticPolicyOutcome.Allow))
        {
            return left;
        }

        var right = EvaluateAuthorization(logical.Right, plan, caller, artifact, subject, properties);
        if (logical.Operator == SemanticLogicalOperator.And && right.Outcome == SemanticPolicyOutcome.Deny) return right;
        if (logical.Operator == SemanticLogicalOperator.Or && right.Outcome == SemanticPolicyOutcome.Allow) return right;

        return left.Outcome == SemanticPolicyOutcome.Unsupported ? left : right;
    }

    static bool EvaluateCondition(
        SemanticPolicyCondition condition,
        SemanticExecutionPlan plan,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties) => condition switch
    {
        SemanticAuthenticatedCondition => caller.Authenticated,
        SemanticRoleCondition role => caller.Roles.Contains(role.Role, StringComparer.Ordinal),
        SemanticClaimCondition claim => MatchClaim(claim, plan, caller, artifact, subject, properties),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical =>
            EvaluateCondition(logical.Left, plan, caller, artifact, subject, properties) && EvaluateCondition(logical.Right, plan, caller, artifact, subject, properties),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical =>
            EvaluateCondition(logical.Left, plan, caller, artifact, subject, properties) || EvaluateCondition(logical.Right, plan, caller, artifact, subject, properties),
        _ => throw new InvalidSemanticContract("Unknown policy condition node or operator.")
    };

    static bool MatchClaim(SemanticClaimCondition claim, SemanticExecutionPlan plan, SemanticCaller caller, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties)
    {
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => claim.Value,
            SemanticClaimTargetKind.Subject => Text(subject),
            SemanticClaimTargetKind.Artifact when claim.Value is not null => Text(ArtifactValue(claim.Value, artifact, plan, properties)),
            _ => null
        };
        return target is not null && caller.Claims.Any(value =>
            string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(value.Value, target, StringComparison.Ordinal));
    }

    static SemanticValue? ArtifactValue(string path, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticExecutionPlan plan, IEnumerable<SemanticProperty> properties)
    {
        var parts = path.Split('.');
        if (!artifact.TryGetValue(parts[0], out var value)) return null;
        var type = properties.Single(property => property.Name == parts[0]).Type;
        foreach (var name in parts.Skip(1))
        {
            if (value is not SemanticCompositeValue composite || type.Kind != SemanticTypeReferenceKind.CompositeType) return null;
            var shape = plan.Model.Application.Types.Single(declaration => declaration.Id == type.Target);
            var member = shape.Properties.Single(property => property.Name == name);
            value = composite.Properties.SingleOrDefault(property => property.TargetProperty == member.Id)?.Value;
            if (value is null) return null;
            type = member.Type;
        }

        return value;
    }

    static string? Text(SemanticValue? value) => value is SemanticTextValue text ? text.Value : null;
}
