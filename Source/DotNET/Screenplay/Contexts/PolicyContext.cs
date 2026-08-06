// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything a policy is given when it decides - the caller, what the decision is about, and the
/// circumstances it is made under.
/// </summary>
/// <param name="Artifact">What the decision is about - the command being authorized, or the arguments of the
/// query being authorized. It is what a <c>claim "&lt;name&gt;" matches &lt;path&gt;</c> condition resolves
/// its path against.</param>
/// <param name="Subject">The identifier of the thing the artifact acts on - the value of the command's
/// <c>identifier</c> property, <see cref="string.Empty"/> when it declares none. It is what a
/// <c>claim "&lt;name&gt;" matches subject</c> condition compares the claim to.</param>
/// <param name="Identity">The <see cref="Contexts.Identity">caller</see> the decision is made about.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the authorized command or query is executing for.</param>
/// <param name="Occurred">When the authorized command or query was received.</param>
/// <remarks>
/// <para>
/// This is the type a policy's inline <c>csharp</c> block compiles against - it is in scope as <c>context</c>.
/// The block answers with a <c>bool</c>, exactly like the declarative <c>require</c> conditions it stands in
/// for, so a policy written in code and a policy written in conditions compose the same way.
/// </para>
/// <para>
/// A policy sees an <see cref="Contexts.Identity"/> rather than a <see cref="CausedBy"/> because a decision
/// needs what the caller can prove - authenticated, roles, claims - and not the three values an appended event
/// records. That also leaves exactly one <c>Subject</c> in scope, and it means the thing being acted on rather
/// than the caller.
/// </para>
/// </remarks>
public record PolicyContext(
    dynamic Artifact,
    string Subject,
    Identity Identity,
    TenantId Tenant,
    DateTimeOffset Occurred);
