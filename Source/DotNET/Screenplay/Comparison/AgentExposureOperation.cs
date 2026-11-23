// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>A bound explicit allowlist entry at one revision.</summary>
/// <param name="Kind">The executable operation kind.</param>
/// <param name="Id">The authoritative identity, or null in address matching mode.</param>
/// <param name="QualifiedName">The canonical qualified operation name.</param>
/// <param name="Scope">The required token scope.</param>
/// <param name="Description">The authored human description.</param>
public sealed record AgentExposureOperation(SemanticAgentOperationKind Kind, SemanticId? Id, string QualifiedName, SemanticAgentScope Scope, string Description);
