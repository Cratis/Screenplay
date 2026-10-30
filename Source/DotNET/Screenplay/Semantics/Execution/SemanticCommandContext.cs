// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies a constrained handler's semantic command and known circumstances.
/// </summary>
/// <param name="Command">The validated command, including supplied generated fixtures, with semantic property ids.</param>
/// <param name="Tenant">The supplied tenant, or null.</param>
/// <param name="Identity">The supplied authorization identity, or null when unavailable.</param>
/// <param name="CausedBy">The supplied audit identity, or null for an absent or time-only occurrence.</param>
/// <param name="Causation">The known cause, or null; the reference request currently supplies no causation.</param>
/// <param name="Occurred">The supplied occurrence time, or null. No clock or sentinel is substituted.</param>
public sealed record SemanticCommandContext(
    SemanticCompositeValue Command,
    string? Tenant,
    SemanticBodyIdentity? Identity,
    CausedBy? CausedBy,
    Causation? Causation,
    DateTimeOffset? Occurred);
