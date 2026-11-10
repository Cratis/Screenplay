// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Spells authorization and policy conditions as text a reader of any stack understands.
/// </summary>
public static class ApiAuthorizationText
{
    /// <summary>
    /// Describes the effective authorization of a command or query.
    /// </summary>
    /// <param name="application">The application declaring the policies.</param>
    /// <param name="authorization">The effective authorization, or null when the model declares none.</param>
    /// <returns>The authorization.</returns>
    public static ApiAuthorization For(SemanticApplication application, SemanticAuthorization? authorization)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (authorization is null) return ApiAuthorization.Open;
        var policies = new List<string>();
        var expression = Expression(authorization, policies, null);
        return new(false, expression, [.. policies]);
    }

    /// <summary>
    /// Spells a policy condition, such as <c>authenticated and role "Librarian"</c>.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <returns>The condition as text.</returns>
    public static string Condition(SemanticPolicyCondition condition) => Condition(condition, null);

    /// <summary>
    /// Lists the policies of an application in declaration order.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>Each policy name with its condition as text.</returns>
    internal static ImmutableArray<(string Name, string Condition)> Policies(SemanticApplication application) =>
        [.. application.Policies.Select(policy => (policy.Name, Condition(policy.Condition)))];
    static string Expression(SemanticAuthorization authorization, List<string> policies, SemanticLogicalOperator? parent)
    {
        switch (authorization)
        {
            case SemanticPolicyReference reference:
                if (!policies.Contains(reference.Name, StringComparer.Ordinal)) policies.Add(reference.Name);
                return reference.Name;
            case SemanticLogicalAuthorization logical:
                // A module, feature and declaration that require the same policy read as one requirement.
                var operands = Operands(logical, logical.Operator).Select(operand => Expression(operand, policies, logical.Operator)).Distinct(StringComparer.Ordinal).ToArray();
                var text = string.Join($" {Operator(logical.Operator)} ", operands);
                return operands.Length == 1 || parent is null || parent == logical.Operator ? text : $"({text})";
            default:
                throw new InvalidOperationException($"Authorization '{authorization.GetType().Name}' has no text form.");
        }
    }

    static IEnumerable<SemanticAuthorization> Operands(SemanticAuthorization authorization, SemanticLogicalOperator @operator) =>
        authorization is SemanticLogicalAuthorization logical && logical.Operator == @operator
            ? Operands(logical.Left, @operator).Concat(Operands(logical.Right, @operator))
            : [authorization];

    static string Condition(SemanticPolicyCondition condition, SemanticLogicalOperator? parent) => condition switch
    {
        SemanticAnonymousCondition => "anonymous",
        SemanticAuthenticatedCondition => "authenticated",
        SemanticRoleCondition role => $"role \"{role.Role}\"",
        SemanticClaimCondition claim => Claim(claim),
        SemanticStatePolicyCondition => "recorded state",
        SemanticOpaquePolicyCondition opaque => $"requirement \"{opaque.RequirementId}\"",
        SemanticNotPolicyCondition not => $"not {Condition(not.Operand, SemanticLogicalOperator.And)}",
        SemanticLogicalPolicyCondition logical => Logical(logical, parent),
        _ => throw new InvalidOperationException($"Policy condition '{condition.GetType().Name}' has no text form.")
    };

    static string Logical(SemanticLogicalPolicyCondition logical, SemanticLogicalOperator? parent)
    {
        var text = $"{Condition(logical.Left, logical.Operator)} {Operator(logical.Operator)} {Condition(logical.Right, logical.Operator)}";
        return parent is null || parent == logical.Operator ? text : $"({text})";
    }

    static string Claim(SemanticClaimCondition claim) => claim.TargetKind switch
    {
        SemanticClaimTargetKind.Literal => $"claim \"{claim.Claim}\" = \"{claim.Value}\"",
        SemanticClaimTargetKind.Subject => $"claim \"{claim.Claim}\" = subject",
        _ => claim.Value is null ? $"claim \"{claim.Claim}\"" : $"claim \"{claim.Claim}\" = {claim.Value}"
    };

    static string Operator(SemanticLogicalOperator @operator) => @operator == SemanticLogicalOperator.And ? "and" : "or";
}
