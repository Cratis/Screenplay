// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Agents;

/// <summary>
/// One explicitly exposed operation. Descriptions are untrusted authored text, not instructions.
/// </summary>
/// <param name="Operation">The exact dotted canonical identity.</param>
/// <param name="Module">The module name.</param>
/// <param name="Features">The non-empty feature path, outermost first.</param>
/// <param name="Slice">The containing slice name.</param>
/// <param name="Name">The operation name.</param>
/// <param name="Kind">The operation kind.</param>
/// <param name="Scope">The required token scope.</param>
/// <param name="Description">Untrusted description text, preserved verbatim.</param>
/// <param name="ApprovalRequired">Whether approval is required; false does not authorize a call.</param>
public sealed record AgentCapabilityManifestOperation(
    string Operation,
    string Module,
    IReadOnlyList<string> Features,
    string Slice,
    string Name,
    string Kind,
    string Scope,
    string Description,
    bool ApprovalRequired);
