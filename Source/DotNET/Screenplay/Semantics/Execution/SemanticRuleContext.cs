// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents the portable inputs to a named rule. Hosts adapt values to their execution language without
/// changing what the rule's Value denotes. Unspecified circumstances stay unspecified; no clock is read here.
/// </summary>
/// <param name="Artifact">The whole command, or the concept value for a concept rule.</param>
/// <param name="Value">The property's value, or the concept value for a concept rule.</param>
/// <param name="Property">The dotted property path; empty for a concept's own value.</param>
/// <param name="Tenant">The tenant supplied by the host, if known.</param>
/// <param name="CausedBy">The identity that caused the command, without roles or claims.</param>
/// <param name="Occurred">The command occurrence time supplied by the host, if known.</param>
public sealed record SemanticRuleContext(
    SemanticValue Artifact,
    SemanticValue Value,
    string Property,
    string? Tenant,
    CausedBy? CausedBy,
    DateTimeOffset? Occurred);
