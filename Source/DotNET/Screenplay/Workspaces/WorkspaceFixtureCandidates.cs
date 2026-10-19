// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The fixture declarations of one syntax index, computed once: fixture-capable entries in index order with
/// only the latest event generation per name and scope, their declarations, scopes, the entry of each declaration
/// and the imports by name.
/// </summary>
sealed class WorkspaceFixtureCandidates
{
    static readonly ConditionalWeakTable<WorkspaceSyntaxIndex, WorkspaceFixtureCandidates> _cache = [];

    readonly WorkspaceSyntaxIndex _index;
    readonly ConcurrentDictionary<WorkspaceNodeHandle, DeclarationScope> _scopes = [];
    readonly Dictionary<string, ImportSyntax[]> _imports;
    readonly Dictionary<Declaration, WorkspaceSyntaxEntry?> _entries = [];

    WorkspaceFixtureCandidates(WorkspaceSyntaxIndex index)
    {
        _index = index;
        var fixtures = index.Entries.Where(entry => entry.Node is SpecificationExampleSyntax or CommandSyntax or EventSyntax or ReadModelSyntax).ToArray();
        var latest = new Dictionary<(string Name, string Scope), uint>();
        foreach (var entry in fixtures)
        {
            if (entry.Node is EventSyntax @event)
            {
                var key = (@event.Name, ScopeKey(entry));
                latest[key] = latest.TryGetValue(key, out var generation) ? Math.Max(generation, @event.Generation) : @event.Generation;
            }
        }

        Entries = [.. fixtures.Where(entry => entry.Node is not EventSyntax historical || latest[(historical.Name, ScopeKey(entry))] <= historical.Generation)
            .Select(entry => (Entry: entry, Declaration: new Declaration(WorkspaceReferenceBindings.Name(entry.Node)!, ScopeOf(entry))))];
        Declarations = new(Entries.Select(entry => entry.Declaration));
        foreach (var (entry, declaration) in Entries)
        {
            // Keeps the scan this replaces: a declaration shared by two candidates resolves to no entry.
            _entries[declaration] = _entries.ContainsKey(declaration) ? null : entry;
        }
        _imports = index.Entries.Select(entry => entry.Node).OfType<ImportSyntax>().GroupBy(import => import.Name).ToDictionary(group => group.Key, group => group.ToArray());
    }

    internal (WorkspaceSyntaxEntry Entry, Declaration Declaration)[] Entries { get; }

    internal ReferenceDeclarationIndex Declarations { get; }

    internal static WorkspaceFixtureCandidates For(WorkspaceSyntaxIndex index) => _cache.GetValue(index, created => new(created));

    internal DeclarationScope ScopeOf(WorkspaceSyntaxEntry entry) =>
        _scopes.GetOrAdd(entry.Handle, static (_, state) => WorkspaceReferenceBindings.Scope(state.Entry, state.Index), (Entry: entry, Index: _index));

    internal WorkspaceSyntaxEntry? EntryOf(Declaration? declaration) => declaration is not null && _entries.TryGetValue(declaration, out var entry) ? entry : null;

    internal ImportSyntax[] ImportsNamed(string name) => _imports.TryGetValue(name, out var imports) ? imports : [];

    string ScopeKey(WorkspaceSyntaxEntry entry) => ReferenceDeclarationIndex.ScopeKey(ScopeOf(entry).Segments);
}
