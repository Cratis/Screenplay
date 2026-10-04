// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Logical authoring keys select exact owners. Source handles identify physical occurrences;
// neither enrolls an executable semantic or requirement identity.
sealed class McpEventSourceInventory
{
    readonly ScreenplayWorkspace _workspace;
    readonly WorkspaceSyntaxIndex _index;
    readonly Dictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _entries;
    readonly Dictionary<SyntaxNode, WorkspaceSyntaxEntry> _nodes;
    readonly Dictionary<string, int> _sourceCounts;
    readonly Dictionary<(string Source, string Stream), int> _streamCounts;

    internal McpEventSourceInventory(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index)
    {
        _workspace = workspace;
        _index = index;
        _entries = index.Entries.ToDictionary(entry => entry.Handle);
        var comparer = (IEqualityComparer<SyntaxNode>)ReferenceEqualityComparer.Instance;
        _nodes = index.Entries.GroupBy(entry => entry.Node, comparer).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), comparer);
        Entries = [.. index.Entries.Where(entry => entry.Node is EventSourceSyntax or EventStreamSyntax)];
        _sourceCounts = Entries.Select(entry => entry.Node).OfType<EventSourceSyntax>().GroupBy(source => source.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        _streamCounts = Entries.Where(entry => entry.Node is EventStreamSyntax).GroupBy(entry => (Source: Scope(entry).FirstOrDefault() ?? string.Empty, Stream: ((EventStreamSyntax)entry.Node).Name)).ToDictionary(group => group.Key, group => group.Count());
    }

    internal WorkspaceSyntaxEntry[] Entries { get; }

    internal string Key(WorkspaceSyntaxEntry entry) => JsonSerializer.Serialize(new
    {
        application = _workspace.IdentityCatalog.Application.ToString(), kind = Kind(entry), scope = Scope(entry), name = Name(entry)
    });

    internal bool AmbiguousOwner(WorkspaceSyntaxEntry entry)
    {
        var scope = Scope(entry);
        if (entry.Node is EventStreamSyntax && scope.Length != 1) return true;
        var parent = entry.Node is EventSourceSyntax source ? source.Name : scope[0];
        return _sourceCounts.GetValueOrDefault(parent) != 1 || (entry.Node is EventStreamSyntax stream && _streamCounts.GetValueOrDefault((parent, stream.Name)) != 1);
    }

    internal object Summary(WorkspaceSyntaxEntry entry) => new
    {
        authoringKey = Key(entry), keyKind = "logical-authoring-only", kind = Kind(entry), name = Name(entry), scope = Scope(entry),
        handle = McpAstHandles.Describe(entry.Handle), entry.Location, ownership = AmbiguousOwner(entry) ? "ambiguous" : "unique",
        identifier = (entry.Node as EventSourceSyntax)?.Identifier, streamId = (entry.Node as EventStreamSyntax)?.StreamId,
        id = entry.Node is EventSourceSyntax source ? source.Id : ((EventStreamSyntax)entry.Node).Id,
        syntaxOnly = true, executionAvailable = false, executionReadiness = "Unavailable until ESM v10 (PLAY0268). Pins are rename-only authored metadata, not semantic identities."
    };

    internal IEnumerable<object> Details(WorkspaceSyntaxEntry entry)
    {
        yield return new { kind = "declaration", declaration = Summary(entry), syntax = entry.Node };
        if (entry.Node is EventSourceSyntax source)
        {
            foreach (var stream in source.Streams)
            {
                if (_nodes.TryGetValue(stream, out var child)) yield return new { kind = "stream", declaration = Summary(child) };
            }
        }
    }

    internal IEnumerable<object> Routes()
    {
        foreach (var entry in _index.Entries.Where(entry => entry.Node is CommandSyntax))
        {
            var command = (CommandSyntax)entry.Node;
            if (command.Stream is null && !command.StreamCandidates.Any()) continue;
            yield return new
            {
                kind = "command-route", command = command.Name, scope = Scope(entry), handle = McpAstHandles.Describe(entry.Handle),
                authoredRoute = command.Stream, ambiguousStreamCandidates = command.StreamCandidates,
                syntaxOnly = true, executionAvailable = false, executionReadiness = "Unavailable until ESM v10 (PLAY0268). Authored routing does not infer identity destinations."
            };
        }
    }

    static string Kind(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax ? "EventSource" : "EventStream";
    static string Name(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax source ? source.Name : ((EventStreamSyntax)entry.Node).Name;

    string[] Scope(WorkspaceSyntaxEntry entry)
    {
        var names = new List<string>();
        for (var parent = entry.Parent; parent is not null && _entries.TryGetValue(parent, out var owner); parent = owner.Parent)
        {
            switch (owner.Node)
            {
                case EventSourceSyntax source: names.Add(source.Name); break;
                case ModuleSyntax module: names.Add(module.Name); break;
                case FeatureSyntax feature: names.Add(feature.Name); break;
                case SliceSyntax slice: names.Add(slice.Name); break;
            }
        }
        names.Reverse();

        return [.. names];
    }
}
