// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Synthesizes the deterministic authenticated witness of a persona's policies.
/// </summary>
public static class PersonaCallers
{
    /// <summary>
    /// Resolves a persona into a caller, or explains why an explicit caller is needed.
    /// </summary>
    /// <param name="persona">The persona to synthesize.</param>
    /// <param name="application">The application declaring its policies.</param>
    /// <returns>The witness, contributions, ambiguities, and refusal.</returns>
    public static PersonaCallerResult Synthesize(PersonaSyntax persona, ApplicationSyntax application)
    {
        var contributions = new List<PersonaCallerContribution>();
        var ambiguities = new List<PersonaCallerAmbiguity>();
        var policies = new List<PolicySyntax>();
        PersonaCallerResult Refused(PersonaCallerRefusal refusal) => new(null, contributions, ambiguities, refusal);
        if (!persona.Policies.Any()) return Refused(new(null, "noPolicies", persona.Location));
        foreach (var name in persona.Policies)
        {
            var policy = application.Policies.FirstOrDefault(policy => policy.Name == name);
            if (policy is null) return Refused(new(name, "unresolvedPolicy", persona.Location));
            if (policy.Condition is null || policy.Code is not null || policy.File is not null) return Refused(new(name, "opaqueImplementation", policy.Location));
            if (Negation(policy.Condition) is { } not) return Refused(new(name, "negation", not.Location));
            policies.Add(policy);
        }

        foreach (var policy in policies)
        {
            if (Required(policy.Condition!, policy.Name, contributions) is { } refusal) return Refused(refusal);
        }
        var required = contributions.ToArray();
        foreach (var policy in policies)
        {
            if (Complete(policy.Condition!, policy.Name, contributions, required, ambiguities) is { } refusal) return Refused(refusal);
        }

        var roles = contributions.Where(value => value.Kind == "role").Select(value => value.Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var claims = new List<SpecificationCallerClaimSyntax>();
        foreach (var contribution in contributions.Where(value => value.Kind == "claim"))
        {
            if (!claims.Exists(claim => string.Equals(claim.Type, contribution.Type, StringComparison.OrdinalIgnoreCase) && claim.Value == contribution.Value))
            {
                claims.Add(new(contribution.Type!, contribution.Value, contribution.Location));
            }
        }
        var caller = new SpecificationCallerSyntax(true, roles, claims.OrderBy(claim => claim.Type, StringComparer.OrdinalIgnoreCase).ThenBy(claim => claim.Value, StringComparer.Ordinal), persona.Location);
        return new(caller, contributions, ambiguities, null);
    }

    static NotPolicyConditionSyntax? Negation(PolicyConditionSyntax condition) => condition switch
    {
        NotPolicyConditionSyntax not => not,
        LogicalPolicyConditionSyntax logical => Negation(logical.Left) ?? Negation(logical.Right),
        _ => null
    };

    static PersonaCallerRefusal? Atom(PolicyConditionSyntax condition, string policy, List<PersonaCallerContribution> values)
    {
        switch (condition)
        {
            case AuthenticatedConditionSyntax:
                values.Add(new("authenticated", "true", null, policy, condition.Location));
                return null;
            case RoleConditionSyntax role:
                values.Add(new("role", role.Role, null, policy, condition.Location));
                return null;
            case ClaimConditionSyntax claim when string.Equals(claim.Claim, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase):
                return new(policy, "roleClaim", claim.Location);
            case ClaimConditionSyntax { MatchesSubject: false, Matches: LiteralExpressionSyntax { Value: string value } } claim:
                values.Add(new("claim", value, claim.Claim, policy, claim.Location));
                return null;
            default:
                return new(policy, "nonLiteralClaim", condition.Location);
        }
    }

    static PersonaCallerRefusal? Required(PolicyConditionSyntax condition, string policy, List<PersonaCallerContribution> values) => condition switch
    {
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.Or } => null,
        LogicalPolicyConditionSyntax logical => Required(logical.Left, policy, values) ?? Required(logical.Right, policy, values),
        _ => Atom(condition, policy, values)
    };

    static bool Satisfied(PolicyConditionSyntax condition, IReadOnlyList<PersonaCallerContribution> values) => condition switch
    {
        AuthenticatedConditionSyntax => true,
        RoleConditionSyntax role => values.Any(value => value.Kind == "role" && value.Value == role.Role),
        ClaimConditionSyntax { MatchesSubject: false, Matches: LiteralExpressionSyntax { Value: string literal } } claim => values.Any(value => value.Kind == "claim" && string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) && value.Value == literal),
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.And } logical => Satisfied(logical.Left, values) && Satisfied(logical.Right, values),
        LogicalPolicyConditionSyntax logical => Satisfied(logical.Left, values) || Satisfied(logical.Right, values),
        _ => false
    };

    static PersonaCallerRefusal? Complete(PolicyConditionSyntax condition, string policy, List<PersonaCallerContribution> values, IReadOnlyList<PersonaCallerContribution>? required = null, List<PersonaCallerAmbiguity>? ambiguities = null)
    {
        if (condition is not LogicalPolicyConditionSyntax logical) return Satisfied(condition, values) ? null : Atom(condition, policy, values);
        if (logical.Operator == LogicalOperator.And) return Complete(logical.Left, policy, values, required, ambiguities) ?? Complete(logical.Right, policy, values, required, ambiguities);
        if (Satisfied(condition, values)) return null;
        List<PersonaCallerContribution> left = [.. values];
        List<PersonaCallerContribution> right = [.. values];
        List<PersonaCallerAmbiguity>? leftAmbiguities = ambiguities is null ? null : [];
        List<PersonaCallerAmbiguity>? rightAmbiguities = ambiguities is null ? null : [];
        var first = Complete(logical.Left, policy, left, required, leftAmbiguities);
        var second = Complete(logical.Right, policy, right, required, rightAmbiguities);
        if (first is not null && second is not null) return first;
        if (first is null)
        {
            if (second is null && required is not null && !Satisfied(condition, required)) ambiguities?.Add(new(policy, logical.Right, logical.Location));
            values.AddRange(left.Skip(values.Count));
            if (leftAmbiguities is not null) ambiguities!.AddRange(leftAmbiguities);
            return null;
        }
        values.AddRange(right.Skip(values.Count));
        if (rightAmbiguities is not null) ambiguities!.AddRange(rightAmbiguities);
        return null;
    }
}

/// <summary>
/// The witness and author-visible explanation of persona synthesis.
/// </summary>
/// <param name="Caller">The authenticated caller, absent on refusal.</param>
/// <param name="Contributions">The atoms contributed by named policies.</param>
/// <param name="Ambiguities">Unpinned choices with more than one buildable alternative.</param>
/// <param name="Refusal">The reason an explicit caller is required.</param>
public sealed record PersonaCallerResult(SpecificationCallerSyntax? Caller, IReadOnlyList<PersonaCallerContribution> Contributions, IReadOnlyList<PersonaCallerAmbiguity> Ambiguities, PersonaCallerRefusal? Refusal);

/// <summary>
/// A policy's contribution to the synthesized caller.
/// </summary>
/// <param name="Kind">Authenticated, role, or claim.</param>
/// <param name="Value">The atom's value.</param>
/// <param name="Type">The claim type, if any.</param>
/// <param name="Policy">The policy supplying this atom.</param>
/// <param name="Location">The policy atom's location.</param>
public sealed record PersonaCallerContribution(string Kind, string Value, string? Type, string Policy, SourceLocation Location);

/// <summary>
/// A compile-time persona synthesis refusal.
/// </summary>
/// <param name="Policy">The blocking policy, absent for a policyless persona.</param>
/// <param name="Reason">The published refusal reason.</param>
/// <param name="Location">The blocking construct's location.</param>
public sealed record PersonaCallerRefusal(string? Policy, string Reason, SourceLocation Location);

/// <summary>
/// An unchosen buildable alternative not pinned by required atoms.
/// </summary>
/// <param name="Policy">The policy containing the choice.</param>
/// <param name="Alternative">The unchosen alternative.</param>
/// <param name="Location">The choice's location.</param>
public sealed record PersonaCallerAmbiguity(string Policy, PolicyConditionSyntax Alternative, SourceLocation Location);
