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
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties) => authorization is null ||
        (caller is { Roles.IsDefault: false, Claims.IsDefault: false } &&
            EvaluateAuthorization(authorization, plan, caller, artifact, subject, properties));

    static bool EvaluateAuthorization(
        SemanticAuthorization authorization,
        SemanticExecutionPlan plan,
        SemanticCaller caller,
        IReadOnlyDictionary<string, SemanticValue> artifact,
        SemanticValue? subject,
        IEnumerable<SemanticProperty> properties) => authorization switch
    {
        SemanticPolicyReference reference => EvaluateCondition(
            plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, plan, caller, artifact, subject, properties),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical =>
            EvaluateAuthorization(logical.Left, plan, caller, artifact, subject, properties) &&
            EvaluateAuthorization(logical.Right, plan, caller, artifact, subject, properties),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical =>
            EvaluateAuthorization(logical.Left, plan, caller, artifact, subject, properties) ||
            EvaluateAuthorization(logical.Right, plan, caller, artifact, subject, properties),
        _ => throw new InvalidSemanticContract("Unknown authorization node or operator.")
    };

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
