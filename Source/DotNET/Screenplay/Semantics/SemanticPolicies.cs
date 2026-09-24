// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>Represents a named, declarative authorization rule.</summary>
/// <param name="Name">The referenced name.</param>
/// <param name="Condition">The condition that must hold.</param>
public sealed record SemanticPolicy(string Name, SemanticPolicyCondition Condition);

/// <summary>Represents a portable authorization condition.</summary>
public abstract record SemanticPolicyCondition;

/// <summary>Requires an authenticated caller.</summary>
public sealed record SemanticAuthenticatedCondition : SemanticPolicyCondition;

/// <summary>Requires a caller role.</summary>
/// <param name="Role">The ordinal, case-sensitive role name.</param>
public sealed record SemanticRoleCondition(string Role) : SemanticPolicyCondition;

/// <summary>Specifies the source against which a claim is matched.</summary>
public enum SemanticClaimTargetKind
{
    /// <summary>The caller's artifact.</summary>
    Artifact = 0,
    /// <summary>A literal value.</summary>
    Literal = 1,
    /// <summary>The acted-on subject.</summary>
    Subject = 2
}

/// <summary>Requires at least one claim value to match the target.</summary>
/// <param name="Claim">The ordinal-ignore-case claim type.</param>
/// <param name="TargetKind">The target source.</param>
/// <param name="Value">The literal or artifact property path; null for subject.</param>
public sealed record SemanticClaimCondition(string Claim, SemanticClaimTargetKind TargetKind, string? Value) : SemanticPolicyCondition;

/// <summary>Combines authorization conditions, preserving grouping.</summary>
/// <param name="Left">The left condition.</param>
/// <param name="Operator">The conjunction or disjunction.</param>
/// <param name="Right">The right condition.</param>
public sealed record SemanticLogicalPolicyCondition(
    SemanticPolicyCondition Left,
    SemanticLogicalOperator Operator,
    SemanticPolicyCondition Right) : SemanticPolicyCondition;

/// <summary>Represents an authorization expression over named policies.</summary>
public abstract record SemanticAuthorization;

/// <summary>References a named authorization policy.</summary>
/// <param name="Name">The policy name.</param>
public sealed record SemanticPolicyReference(string Name) : SemanticAuthorization;

/// <summary>Combines policy references with the authored logical operator.</summary>
/// <param name="Left">The left authorization.</param>
/// <param name="Operator">The logical operator.</param>
/// <param name="Right">The right authorization.</param>
public sealed record SemanticLogicalAuthorization(
    SemanticAuthorization Left,
    SemanticLogicalOperator Operator,
    SemanticAuthorization Right) : SemanticAuthorization;

/// <summary>Represents the explicit caller supplied with an execution request.</summary>
/// <param name="Authenticated">Whether the caller is authenticated.</param>
/// <param name="Roles">Roles held by the caller.</param>
/// <param name="Claims">Multi-valued claims carried by the caller.</param>
public sealed record SemanticCaller(bool Authenticated, ImmutableArray<string> Roles, ImmutableArray<SemanticCallerClaim> Claims);

/// <summary>Represents one caller claim, allowing repeated claim types.</summary>
/// <param name="Type">The claim type.</param>
/// <param name="Value">The claim value.</param>
public sealed record SemanticCallerClaim(string Type, string Value);
