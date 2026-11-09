// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateAgents(SemanticApplication application, SemanticVersion version)
    {
        if (application.Agents is not { } agents) return;
        if (!version.IsAtLeast(SemanticVersion.V44)) throw new InvalidSemanticContract("Agent exposure requires ESM v44 or later.");
        if (agents.TokenKind != SemanticAgentTokenKind.Personal) throw new InvalidSemanticContract("Agent exposure requires personal tokens.");
        if (agents.Operations.IsDefault) throw new InvalidSemanticContract("Agent operations must be an initialized array.");
        var targets = SemanticAgentOperationCatalog.Of(application).Select(target => (target.Kind, target.Id)).ToHashSet();
        var seen = new HashSet<(SemanticAgentOperationKind, SemanticId)>();
        foreach (var operation in agents.Operations) ValidateAgentOperation(operation, targets, seen);
    }

    static void ValidateAgentOperation(SemanticAgentOperation? operation, HashSet<(SemanticAgentOperationKind, SemanticId)> targets, HashSet<(SemanticAgentOperationKind, SemanticId)> seen)
    {
        if (operation is null) throw new InvalidSemanticContract("An agent operation cannot be null.");
        if (!targets.Contains((operation.Kind, operation.Operation))) throw new InvalidSemanticContract($"Agent operation '{operation.Operation}' does not resolve to a {operation.Kind}.");
        if (!seen.Add((operation.Kind, operation.Operation))) throw new InvalidSemanticContract($"Agent operation '{operation.Operation}' is exposed more than once.");
        if (!ValidAgentScope(operation)) throw new InvalidSemanticContract($"Agent operation '{operation.Operation}' has an invalid scope.");
        if (string.IsNullOrWhiteSpace(operation.Description)) throw new InvalidSemanticContract($"Agent operation '{operation.Operation}' requires a description.");
    }

    static bool ValidAgentScope(SemanticAgentOperation operation)
    {
        if (!Enum.IsDefined(operation.Scope)) return false;
        if (operation.Kind == SemanticAgentOperationKind.Command) return operation.Scope == SemanticAgentScope.Write;
        return true;
    }
}
