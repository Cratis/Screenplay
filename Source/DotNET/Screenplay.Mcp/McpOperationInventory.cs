// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Keys name authoring contracts only. Occurrence handles, not these name-derived keys,
// select physical source. Neither is an admitted SemanticId or RequirementId.
sealed class McpOperationInventory
{
    readonly ScreenplayWorkspace _workspace;
    readonly WorkspaceSyntaxIndex _index;
    readonly Dictionary<SyntaxNode, WorkspaceSyntaxEntry> _nodes;

    internal McpOperationInventory(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index)
    {
        _workspace = workspace;
        _index = index;
        var comparer = (IEqualityComparer<SyntaxNode>)ReferenceEqualityComparer.Instance;
        _nodes = index.Entries.GroupBy(entry => entry.Node, comparer)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), comparer);
        Entries = [.. index.Entries.Where(entry => entry.Node is SystemSyntax or OperationSyntax)];
    }

    internal WorkspaceSyntaxEntry[] Entries { get; }

    internal string Key(WorkspaceSyntaxEntry entry) => JsonSerializer.Serialize(new
    {
        application = _workspace.IdentityCatalog.Application.ToString(),
        kind = entry.Node is SystemSyntax ? "System" : "Operation",
        scope = Scope(entry),
        name = entry.Node is SystemSyntax system ? system.Name : ((OperationSyntax)entry.Node).Name
    });

    internal object Summary(WorkspaceSyntaxEntry entry) => new
    {
        authoringKey = Key(entry),
        keyKind = "logical-authoring-only",
        kind = entry.Node is SystemSyntax ? "System" : "Operation",
        name = entry.Node is SystemSyntax system ? system.Name : ((OperationSyntax)entry.Node).Name,
        scope = Scope(entry),
        handle = McpAstHandles.Describe(entry.Handle),
        entry.Location,
        uses = (entry.Node as OperationSyntax)?.Uses,
        inputCount = (entry.Node as OperationSyntax)?.Inputs.Count() ?? 0,
        execute = Phase((entry.Node as OperationSyntax)?.Execute),
        compensate = Phase((entry.Node as OperationSyntax)?.Compensate),
        executionAvailable = false,
        executionReadiness = "Unavailable until ESM v9 (PLAY0268). Authoring intent is not an executable implementation role."
    };

    internal IEnumerable<object> Details(WorkspaceSyntaxEntry entry)
    {
        yield return new { kind = "declaration", declaration = Summary(entry), description = entry.Node is SystemSyntax system ? system.Description : ((OperationSyntax)entry.Node).Description };
        if (entry.Node is not OperationSyntax operation) yield break;
        foreach (var input in operation.Inputs)
        {
            yield return new { kind = "input", input.Name, input.Type, input.Location, handle = Handle(input) };
        }
        foreach (var (name, phase) in new[] { ("execute", operation.Execute), ("compensate", operation.Compensate) })
        {
            if (phase is null) continue;
            yield return new { kind = "phase", phase = name, intent = Phase(phase), phase.Description, phase.Location, handle = Handle(phase) };
            foreach (var hint in phase.Implementation?.Hints ?? [])
            {
                yield return new { kind = "hint", phase = name, hint.Text, hint.Location, handle = Handle(hint) };
            }
        }
    }

    internal IEnumerable<object> Productions()
    {
        var source = McpWorkspaceAnalysis.For(_workspace).Source.Index;
        foreach (var entry in _index.Entries.Where(entry => entry.Node is CommandSyntax))
        {
            var command = (CommandSyntax)entry.Node;
            var scope = Scope(entry);
            var matches = source.Declarations.Where(declaration => declaration.Kind == "Command" && declaration.Name == command.Name && declaration.Scope.SequenceEqual(scope)).Take(2).ToArray();
            foreach (var (production, order) in command.Produces.Select((production, order) => (production, order)))
            {
                var resolution = matches.Length == 1 ? source.Resolve(new(production.Event, ["Event", "Operation"], scope, production.Location, "produces", matches[0].Owner)) : [];
                yield return new
                {
                    kind = "production", command = command.Name, scope, commandHandle = McpAstHandles.Describe(entry.Handle), order,
                    target = production.Event,
                    targetKind = resolution.Length switch { 0 => "Unresolved", 1 => resolution[0].Kind, _ => "Ambiguous" },
                    candidates = resolution.Select(candidate => new { candidate.Kind, candidate.Address }),
                    handle = Handle(production), production.Location, production.When, production.Mappings,
                    executionAvailable = false
                };
            }
        }
    }

    object? Handle(SyntaxNode node) => _nodes.TryGetValue(node, out var entry) ? McpAstHandles.Describe(entry.Handle) : null;

    object? Phase(OperationPhaseSyntax? phase) => phase is null ? null : new
    {
        state = phase switch { { File: not null } => "file", { Code: not null } => "inline", _ => "pending" },
        file = phase.File?.Path,
        language = phase.Code?.Language,
        sourceHandle = phase switch { { File: not null } => Handle(phase.File), { Code: not null } => Handle(phase.Code), _ => null },
        hintCount = phase.Implementation?.Hints.Count() ?? 0,
        executionAvailable = false
    };

    string[] Scope(WorkspaceSyntaxEntry entry)
    {
        var names = new List<string>();
        for (var parent = entry.Parent; parent is not null; parent = _index.Find(parent)?.Parent)
        {
            var node = _index.Find(parent)?.Node;
            if (node is ModuleSyntax module) names.Add(module.Name);
            if (node is FeatureSyntax feature) names.Add(feature.Name);
            if (node is SliceSyntax slice) names.Add(slice.Name);
        }
        names.Reverse();

        return [.. names];
    }
}
