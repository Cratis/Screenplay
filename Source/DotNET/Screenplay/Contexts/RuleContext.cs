// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything a validation rule is given when it runs - what is being validated and the
/// circumstances the decision is made under.
/// </summary>
/// <param name="Artifact">The whole thing under validation - the command a <c>validate</c> block belongs to,
/// or the concept's own value when the rule is declared on a concept. Never null; it is the widest thing the
/// rule can see.</param>
/// <param name="Value">The value the rule is declared on. The property's value for a property rule, the
/// concept's own value for a concept rule, and the <paramref name="Artifact"/> itself for a
/// <c>validate csharp</c> block covering the whole artifact.</param>
/// <param name="Property">The dotted path of <paramref name="Value"/> within <paramref name="Artifact"/>,
/// <see cref="string.Empty"/> when the rule covers the whole artifact.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the validated command or query is executing for.</param>
/// <param name="CausedBy">The <see cref="Contexts.CausedBy">identity</see> that caused the validated command
/// or query to run.</param>
/// <param name="Occurred">When the validated command or query was received.</param>
/// <remarks>
/// <para>
/// This is the type a named <c>rule</c> predicate's inline <c>csharp</c> block, its <c>file</c> reference, and
/// a <c>validate csharp</c> block all compile against - it is in scope as <c>context</c>. A named rule's body
/// answers with a <c>bool</c>; a <c>validate csharp</c> block answers with the message of every rule the
/// artifact breaks, and says nothing when the artifact is valid.
/// </para>
/// <para>
/// A rule sees <see cref="CausedBy"/> but deliberately not an <see cref="Identity"/>: rejecting a specific
/// input because of who sent it - "you may not approve your own request" - is validation and needs the
/// caller's identifier, while inspecting the caller's roles or claims is an authorization decision and belongs
/// in a <c>policy</c> with its <see cref="PolicyContext"/>. Leaving roles and claims out of this context is
/// what keeps the two apart.
/// </para>
/// </remarks>
public record RuleContext(
    dynamic Artifact,
    dynamic Value,
    string Property,
    TenantId Tenant,
    CausedBy CausedBy,
    DateTimeOffset Occurred)
{
    /// <summary>
    /// Gets a value indicating whether the rule covers the whole <see cref="Artifact"/> rather than one of its
    /// properties, which is the case for a <c>validate csharp</c> block.
    /// </summary>
    public bool IsWholeArtifact => Property.Length == 0;
}
