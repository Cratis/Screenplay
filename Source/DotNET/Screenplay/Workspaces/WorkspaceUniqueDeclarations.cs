// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The declarations of one syntax index by kind and name, computed once per kind: a name resolves to its declaration
/// only when exactly one declaration of that kind carries it, so a duplicated name stays unresolved.
/// </summary>
sealed class WorkspaceUniqueDeclarations
{
    static readonly ConditionalWeakTable<WorkspaceSyntaxIndex, WorkspaceUniqueDeclarations> _cache = [];

    readonly WorkspaceSyntaxIndex _index;
    readonly ConcurrentDictionary<Type, Dictionary<string, SyntaxNode?>> _byKind = [];

    WorkspaceUniqueDeclarations(WorkspaceSyntaxIndex index) => _index = index;

    internal static WorkspaceUniqueDeclarations For(WorkspaceSyntaxIndex index) => _cache.GetValue(index, created => new(created));

    internal T? Named<T>(string? name)
        where T : SyntaxNode
    {
        if (name is null) return null;
        var byName = _byKind.GetOrAdd(typeof(T), static (_, index) => ByName<T>(index), _index);

        return byName.TryGetValue(name, out var declaration) ? declaration as T : null;
    }

    static Dictionary<string, SyntaxNode?> ByName<T>(WorkspaceSyntaxIndex index)
        where T : SyntaxNode
    {
        var byName = new Dictionary<string, SyntaxNode?>(StringComparer.Ordinal);
        foreach (var node in index.Entries.Select(entry => entry.Node).OfType<T>())
        {
            if (WorkspaceReferenceBindings.Name(node) is not { } name) continue;

            // A second declaration of the name makes it ambiguous: it stays in the map as null.
            byName[name] = byName.ContainsKey(name) ? null : node;
        }

        return byName;
    }
}
