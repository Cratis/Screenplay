// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies the semantic artifact and known circumstances to a policy predicate, without target DTOs.
/// </summary>
/// <param name="Artifact">The command properties or query key arguments, identified by their semantic ids.</param>
/// <param name="Subject">The explicit command identifier or single query key as invariant scalar text, or null when unavailable.</param>
/// <param name="Identity">The authorization view of the supplied caller, without invented audit names.</param>
/// <param name="Tenant">The supplied tenant, or null when unavailable.</param>
/// <param name="Occurred">The supplied occurrence time, or null when unavailable.</param>
public sealed record SemanticPolicyContext(
    SemanticCompositeValue Artifact,
    string? Subject,
    SemanticBodyIdentity Identity,
    string? Tenant,
    DateTimeOffset? Occurred);
