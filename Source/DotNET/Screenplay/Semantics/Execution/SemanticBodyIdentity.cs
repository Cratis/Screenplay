// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents the semantic caller's authentication, ordered roles and repeated ordinary claims.
/// </summary>
/// <param name="IsAuthenticated">Whether the caller is authenticated.</param>
/// <param name="Roles">Explicit roles followed by standard role-claim values, preserving order and duplicates.</param>
/// <param name="Claims">Ordinary claims, excluding normalized role claims.</param>
public sealed record SemanticBodyIdentity(bool IsAuthenticated, ImmutableArray<string> Roles, ImmutableArray<Contexts.Claim> Claims)
{
    /// <summary>
    /// Checks for an ordinal, case-sensitive role.
    /// </summary>
    /// <param name="role">The role to find.</param>
    /// <returns>Whether the caller holds the role.</returns>
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    /// <summary>
    /// Checks for a claim by ordinal-ignore-case name.
    /// </summary>
    /// <param name="name">The claim name.</param>
    /// <returns>Whether the caller carries the claim.</returns>
    public bool HasClaim(string name) => ClaimsNamed(name).Any();

    /// <summary>
    /// Gets the first claim value by ordinal-ignore-case name.
    /// </summary>
    /// <param name="name">The claim name.</param>
    /// <returns>The first value, or null.</returns>
    public string? ClaimValue(string name) => ClaimsNamed(name).Select(claim => claim.Value).FirstOrDefault();

    /// <summary>
    /// Gets every claim value by ordinal-ignore-case name.
    /// </summary>
    /// <param name="name">The claim name.</param>
    /// <returns>The ordered, unmodified values.</returns>
    public IEnumerable<string> ClaimValues(string name) => ClaimsNamed(name).Select(claim => claim.Value);

    internal static SemanticBodyIdentity From(SemanticCaller caller) => new(
        caller.Authenticated,
        caller.Roles.AddRange(caller.Claims.Where(IsRole).Select(claim => claim.Value)),
        [.. caller.Claims.Where(claim => !IsRole(claim)).Select(claim => new Contexts.Claim(claim.Type, claim.Value))]);

    static bool IsRole(SemanticCallerClaim claim) => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase);
    IEnumerable<Contexts.Claim> ClaimsNamed(string name) => Claims.Where(claim => string.Equals(claim.Name, name, StringComparison.OrdinalIgnoreCase));
}
