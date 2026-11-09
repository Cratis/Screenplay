// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>Validates explicit agent exposure against the composed application's existing operations.</summary>
internal static class AgentsValidator
{
    static readonly DeclarationScope _root = new([]);

    public static void Validate(ApplicationSyntax application, ParserContext context, bool deferImportedReferences)
    {
        if (application.Agents is not { } agents) return;
        ValidateTokens(agents, context);
        var targets = AgentOperationCatalog.Of(application);
        var exposed = new HashSet<(AgentOperationKind Kind, string Name)>();
        var deferred = deferImportedReferences && application.FileImports.Any();
        foreach (var operation in agents.Operations)
        {
            ValidateFields(operation, context);
            if (!deferred) ValidateReference(operation, targets, exposed, context);
        }

        if (deferred || context.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) return;
        ReportUnexposed(agents, targets, exposed, context);
    }

    static void ReportUnexposed(AgentsSyntax agents, IReadOnlyList<AgentOperationCatalog.Target> targets, HashSet<(AgentOperationKind Kind, string Name)> exposed, ParserContext context)
    {
        if (!agents.Operations.Any())
        {
            context.Warning(DiagnosticCodes.AgentsExposeNothing, "The agents block exposes no operations", agents.Location);
        }

        var unexposed = targets.Where(target => !exposed.Contains((target.Kind, target.QualifiedName))).Select(target => target.QualifiedName).Order(StringComparer.Ordinal).ToArray();
        if (unexposed.Length > 0)
        {
            var suffix = unexposed.Length > 10 ? $", and {unexposed.Length - 10} more" : string.Empty;
            context.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.UnexposedAgentOperations, $"{unexposed.Length} of {targets.Count} queries and commands are not exposed to agents: {string.Join(", ", unexposed.Take(10))}{suffix}", agents.Location));
        }
    }

    static void ValidateTokens(AgentsSyntax agents, ParserContext context)
    {
        if (agents.Tokens is null)
        {
            context.Error(DiagnosticCodes.AgentTokensMissing, "An agents block requires exactly one 'tokens personal' declaration", agents.Location);
        }
        else if (agents.Tokens.Kind != AgentTokenKind.Personal)
        {
            context.Error(DiagnosticCodes.UnsupportedAgentTokenKind, "Agents currently support only 'tokens personal'", agents.Tokens.Location);
        }
    }

    static void ValidateFields(AgentOperationSyntax operation, ParserContext context)
    {
        if (string.IsNullOrWhiteSpace(operation.Description))
        {
            context.Error(DiagnosticCodes.AgentOperationMissingDescription, "An exposed operation requires a nonempty literal description", operation.Location);
        }

        if (operation.Scope is null)
        {
            context.Error(DiagnosticCodes.AgentOperationMissingScope, "An exposed operation requires an explicit 'scope read' or 'scope write'", operation.Location);
        }
        else if (operation.Kind == AgentOperationKind.Command && operation.Scope != AgentScope.Write)
        {
            context.Error(DiagnosticCodes.AgentCommandRequiresWriteScope, "An exposed command requires 'scope write'", operation.Location);
        }
    }

    static void ValidateReference(AgentOperationSyntax operation, IReadOnlyList<AgentOperationCatalog.Target> targets, HashSet<(AgentOperationKind Kind, string Name)> exposed, ParserContext context)
    {
        var candidates = targets.Where(target => target.Kind == operation.Kind).ToArray();
        var resolution = ReferenceResolver.Resolve(operation.Reference, _root, [.. candidates.Select(target => target.Declaration)]);
        if (resolution.IsUnresolved)
        {
            context.Error(DiagnosticCodes.UnknownAgentOperation, $"Unknown agent {operation.Kind.ToString().ToLowerInvariant()} '{operation.Reference}'", operation.Location);
            return;
        }

        if (resolution.Resolved is not { } declaration)
        {
            var names = resolution.Ambiguous.Select(candidate => string.Join('.', candidate.Scope.Segments.Append(candidate.Name)));
            context.Error(DiagnosticCodes.AmbiguousAgentOperation, $"Ambiguous agent operation '{operation.Reference}' - qualify one of: {string.Join(", ", names)}", operation.Location);
            return;
        }

        var target = candidates.Single(candidate => candidate.Declaration == declaration);
        if (!exposed.Add((operation.Kind, target.QualifiedName)))
        {
            context.Error(DiagnosticCodes.DuplicateAgentOperation, $"Agent operation '{target.QualifiedName}' is exposed more than once", operation.Location);
        }

        if (target.RefusedQueryKind is { } queryKind)
        {
            context.Error(DiagnosticCodes.UnsupportedAgentQueryKind, $"Query '{target.QualifiedName}' is a {queryKind} and cannot be exposed to agents yet", operation.Location);
        }

        if (target.IsGlobal)
        {
            context.Error(DiagnosticCodes.AgentGlobalQueryRefused, $"Global query '{target.QualifiedName}' cannot be exposed to agents", operation.Location);
        }

        if (!target.Authorized)
        {
            context.Warning(DiagnosticCodes.UngatedAgentOperation, $"Exposed {operation.Kind.ToString().ToLowerInvariant()} '{target.QualifiedName}' has no authorization - any token holder can call it", operation.Location);
        }
    }
}
