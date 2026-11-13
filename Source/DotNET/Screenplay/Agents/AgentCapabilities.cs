// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Agents;

/// <summary>The agent surface of a successful complete semantic compilation, never an inferred default.</summary>
/// <param name="TokenKind">The admitted token kind.</param>
/// <param name="Operations">The explicit operation allowlist.</param>
public sealed record AgentCapabilities(AgentTokenKind TokenKind, IReadOnlyList<AgentCapability> Operations)
{
    /// <summary>Projects agent exposure only when the complete compilation succeeded and declares an agents block.</summary>
    /// <param name="compilation">The complete compilation result, including source and semantic diagnostics.</param>
    /// <returns>The explicit surface, or null for an absent block or any compilation error.</returns>
    public static AgentCapabilities? From(CompilationResult<SemanticCompilation> compilation)
    {
        if (!compilation.Success || compilation.Value?.Model.Application.Agents is not { } agents) return null;
        var targets = SemanticAgentOperationCatalog.Of(compilation.Value.Model.Application).ToDictionary(target => (target.Kind, target.Id));
        var operations = agents.Operations.Select(operation =>
        {
            var target = targets[(operation.Kind, operation.Operation)];
            return new AgentCapability(
                operation.Kind == SemanticAgentOperationKind.Query ? AgentOperationKind.Query : AgentOperationKind.Command,
                target.Name,
                target.Path,
                target.QualifiedName,
                operation.Scope == SemanticAgentScope.Read ? AgentScope.Read : AgentScope.Write,
                operation.Description,
                target.IsObservable);
        }).OrderBy(operation => operation.QualifiedName, StringComparer.Ordinal).ThenBy(operation => operation.Kind).ToImmutableArray();
        return new(AgentTokenKind.Personal, operations);
    }
}
