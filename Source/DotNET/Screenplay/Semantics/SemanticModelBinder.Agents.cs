// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        internal SemanticAgents? BindAgents(SemanticApplication application)
        {
            if (syntax.Agents is not { } agents) return null;
            var targets = SemanticAgentOperationCatalog.Of(application).ToArray();
            var declarations = AgentOperationCatalog.Of(syntax);
            var operations = ImmutableArray.CreateBuilder<SemanticAgentOperation>();
            foreach (var operation in agents.Operations)
            {
                var qualifiedName = ResolveAgentName(operation, declarations);
                var kind = operation.Kind == AgentOperationKind.Query ? SemanticAgentOperationKind.Query : SemanticAgentOperationKind.Command;
                var target = targets.SingleOrDefault(target => target.Kind == kind && target.QualifiedName == qualifiedName);
                if (target is null)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Agent operation '{operation.Reference}' has no executable operation identity.", operation.Location);
                    continue;
                }

                var scope = operation.Scope == AgentScope.Read ? SemanticAgentScope.Read : SemanticAgentScope.Write;
                operations.Add(new(kind, target.Id, scope, operation.Description!));
            }

            return new(SemanticAgentTokenKind.Personal, operations.ToImmutable());
        }

        static string? ResolveAgentName(AgentOperationSyntax operation, IReadOnlyList<AgentOperationCatalog.Target> declarations)
        {
            var candidates = declarations.Where(target => target.Kind == operation.Kind).Select(target => target.Declaration);
            var resolved = ReferenceResolver.Resolve(operation.Reference, new DeclarationScope([]), [.. candidates]).Resolved;
            return resolved is null ? null : string.Join('.', resolved.Scope.Segments.Append(resolved.Name));
        }
    }
}
