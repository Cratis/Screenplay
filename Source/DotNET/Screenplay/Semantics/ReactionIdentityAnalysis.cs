// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Compares a declared system identity with portable authorization gates without guessing claims or opaque code.
/// </summary>
internal static class ReactionIdentityAnalysis
{
    internal static bool? Allows(SemanticAuthorization? gate, IEnumerable<SemanticPolicy> policies, IReadOnlySet<string> roles) =>
        Authorization(gate, policies, role => roles.Contains(role), true);

    internal static bool Requires(SemanticAuthorization? gate, IEnumerable<SemanticPolicy> policies, string role) =>
        Authorization(gate, policies, candidate => candidate == role ? false : null, null) == false;

    internal static bool Opaque(SemanticAuthorization? gate, IEnumerable<SemanticPolicy> policies) => gate switch
    {
        SemanticPolicyReference reference => policies.FirstOrDefault(policy => policy.Name == reference.Name)?.Condition is not { } condition || Opaque(condition),
        SemanticLogicalAuthorization logical => Opaque(logical.Left, policies) || Opaque(logical.Right, policies),
        null => false,
        _ => true
    };

    internal static IEnumerable<string> Roles(SemanticAuthorization? gate, IEnumerable<SemanticPolicy> policies) => gate switch
    {
        SemanticPolicyReference reference => policies.FirstOrDefault(policy => policy.Name == reference.Name) is { } policy ? Roles(policy.Condition) : [],
        SemanticLogicalAuthorization logical => Roles(logical.Left, policies).Concat(Roles(logical.Right, policies)),
        _ => []
    };

    static bool Opaque(SemanticPolicyCondition condition) => condition switch
    {
        SemanticOpaquePolicyCondition => true,
        SemanticNotPolicyCondition not => Opaque(not.Operand),
        SemanticLogicalPolicyCondition logical => Opaque(logical.Left) || Opaque(logical.Right),
        _ => false
    };

    static IEnumerable<string> Roles(SemanticPolicyCondition condition) => condition switch
    {
        SemanticRoleCondition role => [role.Role],
        SemanticNotPolicyCondition not => Roles(not.Operand),
        SemanticLogicalPolicyCondition logical => Roles(logical.Left).Concat(Roles(logical.Right)),
        _ => []
    };

    static bool? Authorization(SemanticAuthorization? gate, IEnumerable<SemanticPolicy> policies, Func<string, bool?> role, bool? authenticated) => gate switch
    {
        null => true,
        SemanticPolicyReference reference => policies.FirstOrDefault(policy => policy.Name == reference.Name) is { } policy ? Condition(policy.Condition, role, authenticated) : null,
        SemanticLogicalAuthorization logical => Combine(Authorization(logical.Left, policies, role, authenticated), logical.Operator, Authorization(logical.Right, policies, role, authenticated)),
        _ => null
    };

    static bool? Condition(SemanticPolicyCondition condition, Func<string, bool?> role, bool? authenticated) => condition switch
    {
        SemanticAuthenticatedCondition => authenticated,
        SemanticRoleCondition required => role(required.Role),
        SemanticNotPolicyCondition not => !Condition(not.Operand, role, authenticated),
        SemanticLogicalPolicyCondition logical => Combine(Condition(logical.Left, role, authenticated), logical.Operator, Condition(logical.Right, role, authenticated)),
        _ => null
    };

    static bool? Combine(bool? left, SemanticLogicalOperator op, bool? right)
    {
        if (op == SemanticLogicalOperator.And)
        {
            if (left == false || right == false) return false;
            return left == true && right == true ? true : null;
        }
        if (left == true || right == true) return true;
        return left == false && right == false ? false : null;
    }
}
