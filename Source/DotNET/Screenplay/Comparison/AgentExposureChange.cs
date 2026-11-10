// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison;

/// <summary>One explicitly exposed operation, identified by kind and resolved qualified name.</summary>
/// <param name="Kind">The query or command kind.</param>
/// <param name="QualifiedName">The resolved module, feature, slice and operation name, or the unresolved authored reference.</param>
/// <param name="Change">The conservative structural classification.</param>
/// <param name="Reason">Why this classification was made.</param>
/// <param name="BeforeScope">The baseline token scope, if exposed.</param>
/// <param name="AfterScope">The candidate token scope, if exposed.</param>
/// <param name="DiagnosticCode">The comparison warning for added or widened exposure, if any.</param>
public sealed record AgentExposureChange(AgentOperationKind Kind, string QualifiedName, AgentExposureChangeKind Change, string Reason, AgentScope? BeforeScope, AgentScope? AfterScope, string? DiagnosticCode);
