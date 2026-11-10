// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

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

/// <summary>Represents a named, declarative authorization rule.</summary>
/// <param name="Name">The referenced name.</param>
/// <param name="Condition">The condition that must hold.</param>
public sealed record SemanticPolicy(string Name, SemanticPolicyCondition Condition)
{
    /// <summary>
    /// Gets the read models the policy decides from, empty for a policy that decides from the caller and the artifact alone.
    /// </summary>
    public ImmutableArray<SemanticPolicyRead> Reads { get; init; } = [];

    /// <summary>
    /// Gets the typed inputs the policy declares (ESM v46), empty when it declares none.
    /// </summary>
    public ImmutableArray<SemanticPolicyInput> Inputs { get; init; } = [];
}

/// <summary>
/// A typed input of a policy, which a policy read can take its key from.
/// </summary>
/// <param name="Name">The input name.</param>
/// <param name="Type">The declared type.</param>
public sealed record SemanticPolicyInput(string Name, SemanticTypeReference Type);

/// <summary>
/// An explicit mapping of a policy input to a property of the authorized command or query.
/// </summary>
/// <param name="Input">The policy input.</param>
/// <param name="Property">The name of the artifact property that fills it.</param>
public sealed record SemanticPolicyInputMapping(string Input, string Property);

/// <summary>
/// A read model a policy consults, keyed by a property of the command or query it authorizes.
/// </summary>
/// <param name="Name">The alias state conditions use to name the read.</param>
/// <param name="ReadModel">The read model that is read.</param>
/// <param name="By">The name of the property of the authorized command or query that holds the read model's key.</param>
public sealed record SemanticPolicyRead(string Name, SemanticId ReadModel, string By);

/// <summary>
/// A policy condition that compares recorded state, which is read through the policy's <see cref="SemanticPolicy.Reads"/>.
/// </summary>
/// <param name="Condition">The comparison, over read aliases and constants.</param>
public sealed record SemanticStatePolicyCondition(SemanticCondition Condition) : SemanticPolicyCondition;

/// <summary>Represents a portable authorization condition or an opaque implementation attachment.</summary>
public abstract record SemanticPolicyCondition;

/// <summary>
/// Negates a portable policy condition.
/// </summary>
/// <param name="Operand">The condition to negate.</param>
public sealed record SemanticNotPolicyCondition(SemanticPolicyCondition Operand) : SemanticPolicyCondition;

/// <summary>Represents a policy predicate supplied by a target provider rather than the reference executor.</summary>
/// <param name="RequirementId">The stable identity of the policy implementation requirement.</param>
public sealed record SemanticOpaquePolicyCondition(string RequirementId) : SemanticPolicyCondition;

/// <summary>Admits every caller, including one without an identity, a system identity and a reaction's identity.</summary>
public sealed record SemanticAnonymousCondition : SemanticPolicyCondition;

/// <summary>Requires an authenticated caller.</summary>
public sealed record SemanticAuthenticatedCondition : SemanticPolicyCondition;

/// <summary>Requires a caller role.</summary>
/// <param name="Role">The ordinal, case-sensitive role name.</param>
public sealed record SemanticRoleCondition(string Role) : SemanticPolicyCondition;

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
public sealed record SemanticPolicyReference(string Name) : SemanticAuthorization
{
    /// <summary>
    /// Gets the explicit input mappings (ESM v46), empty when inputs are filled by name.
    /// </summary>
    public ImmutableArray<SemanticPolicyInputMapping> Mappings { get; init; } = [];
}

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
