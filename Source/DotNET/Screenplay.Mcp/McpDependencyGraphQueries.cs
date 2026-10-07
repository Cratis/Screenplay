// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Dependencies;

namespace Cratis.Screenplay.Mcp;

static class McpDependencyGraphQueries
{
    internal static DependencyGraph Graph(McpSnapshot snapshot) => snapshot.DependencyGraph;

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var graph = Graph(snapshot);
        var from = McpJson.OptionalString(arguments, "from") ?? "module";
        var to = McpJson.OptionalString(arguments, "to") ?? "module";
        var view = McpJson.OptionalString(arguments, "view") ?? "edges";
        var direction = McpJson.OptionalString(arguments, "direction") ?? "outgoing";
        var scope = McpJson.OptionalString(arguments, "scope");
        var includeTestOnly = McpJson.Boolean(arguments, "includeTestOnly");
        var evidenceLimit = McpJson.Integer(arguments, "evidenceLimit", 3, 0, 20);
        var kinds = arguments.TryGetProperty("kinds", out var selected) ? selected.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : throw new McpFailure("Dependency kinds must be strings.", -32602)).ToArray() : null;
        if (kinds?.Any(kind => !DependencyGraph.Kinds.Contains(kind, StringComparer.Ordinal)) == true) throw new McpFailure("Unsupported dependency kind.", -32602);
        if (scope is not null && !graph.Nodes.Any(node => node.Address == scope)) throw new McpFailure("Dependency scope must name a module, feature, slice or context.", -32602);
        bool InScope(DependencyNode node) => scope is null || node.Address == scope || node.Address.StartsWith(scope + ".", StringComparison.Ordinal);
        var items = view switch
        {
            "edges" => graph.Implied(from, to, kinds, includeTestOnly, evidenceLimit).Where(edge => InScope(direction == "incoming" ? edge.Target : edge.Source)).Select(edge => (object)new
            {
                source = Node(edge.Source), target = Node(edge.Target), edge.SliceEdges, edge.References, edge.ByKind,
                consumers = edge.Consumers.Select(Node).ToArray(), producers = edge.Producers.Select(Node).ToArray(),
                evidence = edge.Evidence.Select(item => new
                {
                    consumer = Node(item.Consumer), producer = Node(item.Producer), item.Kind, item.Role, item.Name, item.Ambiguous,
                    alternatives = item.Alternatives.Select(Node).ToArray(), item.TestOnly, item.Location
                }).ToArray(), edge.EvidenceCount, edge.EvidenceTruncated
            }),
            "cycles" => (from == to ? graph.Cycles(from, kinds) : []).Where(group => group.Members.Any(InScope)).Select(group => (object)new { members = group.Members.Select(Node).ToArray() }),
            "order" => graph.SuggestedOrder(kinds).Containers.Where(order => InScope(order.Container)).Select(order => (object)new
            {
                container = Node(order.Container), children = order.Children.Select(Node).ToArray(), order.Changed
            }),
            "unresolved" => graph.Unresolved.Where(item => InScope(item.Consumer) && (includeTestOnly || item.Kind != "verifiedWith") && (kinds?.Contains(item.Kind, StringComparer.Ordinal) != false))
                .Select(item => (object)new { consumer = Node(item.Consumer), item.Kind, item.Role, item.Name, item.Location }),
            _ => throw new McpFailure("Unsupported dependency graph view.", -32602)
        };

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            graph.OrderSource,
            coverage = new
            {
                description = "Explicit slice references only; not code, expressions or transitive runtime impact. Earliest authored producer wins, ignoring case; ambiguity retains alternatives. Order and cycles use usesFactsFrom, reactsTo and decidesFrom only; suggestions never edit source.",
                graph.ExcludedReferences, unresolvedCount = graph.Unresolved.Count, graph.UnusedImports
            },
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            page = McpPaging.Page(items, arguments, snapshot.SourceRevision)
        };
    }

    static object Node(DependencyNode node) => new
    {
        kind = node.Kind.Length == 0 ? node.Kind : char.ToUpperInvariant(node.Kind[0]) + node.Kind[1..],
        node.Address
    };
}
