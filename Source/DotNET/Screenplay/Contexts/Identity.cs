// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents the caller a decision is made about - who they are and what they can prove.
/// </summary>
/// <param name="Id">The stable identifier of the caller from the identity provider.</param>
/// <param name="Name">The display name of the caller.</param>
/// <param name="UserName">The user name of the caller.</param>
/// <param name="IsAuthenticated">Whether the caller is authenticated, matching the <c>require authenticated</c> condition.</param>
/// <param name="Roles">The roles the caller holds, matching the <c>require role "&lt;name&gt;"</c> condition.</param>
/// <param name="Claims">The <see cref="Contexts.Claim">claims</see> the caller carries.</param>
/// <remarks>
/// <para>
/// This is the authorization view of the caller, and it is what a <see cref="PolicyContext"/> decides on.
/// <see cref="CausedBy"/> is the audit view of the same caller - the three values that travel with an
/// appended event and that a projection reads through <c>$causedBy</c>. <see cref="Id"/> and
/// <see cref="CausedBy.Subject"/> are the same value seen from the two sides.
/// </para>
/// <para>
/// Roles and claim names are matched with ordinal, case sensitive comparison - the values come from a token
/// and mean exactly what they say.
/// </para>
/// </remarks>
public record Identity(
    string Id,
    string Name,
    string UserName,
    bool IsAuthenticated,
    IEnumerable<string> Roles,
    IEnumerable<Claim> Claims)
{
    /// <summary>
    /// The absence of a known caller - an unauthenticated or system originated call.
    /// </summary>
    public static readonly Identity NotSet = new(string.Empty, string.Empty, string.Empty, false, [], []);

    /// <summary>
    /// Checks whether the caller holds a role.
    /// </summary>
    /// <param name="role">The name of the role to check for.</param>
    /// <returns>True when the caller holds the role, false otherwise.</returns>
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    /// <summary>
    /// Checks whether the caller carries a claim, regardless of its value.
    /// </summary>
    /// <param name="name">The name of the claim to check for.</param>
    /// <returns>True when the caller carries the claim, false otherwise.</returns>
    public bool HasClaim(string name) => ClaimsNamed(name).Any();

    /// <summary>
    /// Gets the value of a claim.
    /// </summary>
    /// <param name="name">The name of the claim to read.</param>
    /// <returns>The value of the first claim with the name, or <c>null</c> when the caller does not carry it.</returns>
    /// <remarks>
    /// Use <see cref="ClaimValues(string)"/> when the caller may carry the same claim more than once and every
    /// value matters.
    /// </remarks>
    public string? ClaimValue(string name) => ClaimsNamed(name).Select(claim => claim.Value).FirstOrDefault();

    /// <summary>
    /// Gets every value of a claim the caller carries more than once.
    /// </summary>
    /// <param name="name">The name of the claim to read.</param>
    /// <returns>The values of every claim with the name, empty when the caller does not carry it.</returns>
    public IEnumerable<string> ClaimValues(string name) => ClaimsNamed(name).Select(claim => claim.Value);

    IEnumerable<Claim> ClaimsNamed(string name) => Claims.Where(claim => string.Equals(claim.Name, name, StringComparison.Ordinal));
}
