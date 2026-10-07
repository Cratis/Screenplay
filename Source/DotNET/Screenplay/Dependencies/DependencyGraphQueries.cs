// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Dependency aggregation, cycle analysis and presentation-order suggestions.
/// </summary>
internal sealed partial class DependencyGraph
{
    /// <summary>
    /// Aggregates edges at any pair of levels, excluding equal or overlapping containers.
    /// </summary>
    /// <param name="from">Source level: slice, feature or module.</param>
    /// <param name="to">Target level: slice, feature, module or context.</param>
    /// <param name="kinds">Optional kinds; by default all except test-only references.</param>
    /// <param name="includeTestOnly">Whether specification references may be included.</param>
    /// <param name="evidenceLimit">Maximum evidence per edge; counts always include all references.</param>
    /// <returns>Deterministically ordered implied dependencies.</returns>
    /// <exception cref="InvalidDependencyQuery">The level, kind or evidence limit is unsupported.</exception>
    public IReadOnlyList<ImpliedDependency> Implied(string from, string to, IEnumerable<string>? kinds = null, bool includeTestOnly = false, int evidenceLimit = 3)
    {
        if (from != "slice" && from != "feature" && from != "module") throw new InvalidDependencyQuery($"Unsupported source level '{from}'.");
        if (to != "slice" && to != "feature" && to != "module" && to != "context") throw new InvalidDependencyQuery($"Unsupported target level '{to}'.");
        if (evidenceLimit < 0) throw new InvalidDependencyQuery("Evidence limit must not be negative.");
        var entries = Evidence(kinds, includeTestOnly).SelectMany(item => At(item.Consumer, from).SelectMany(source => At(item.Producer, to)
            .Where(target => Disjoint(source, target)).Select(target => (Source: source, Target: target, Evidence: item))));

        return [.. entries.GroupBy(item => (item.Source.Key, item.Target.Key)).OrderBy(group => group.First().Source.Rank).ThenBy(group => group.First().Target.Rank)
            .ThenBy(group => group.Key.Item1, StringComparer.Ordinal).ThenBy(group => group.Key.Item2, StringComparer.Ordinal)
            .Select(group =>
            {
                var evidence = OrderedEvidence(group.Select(item => item.Evidence)).ToArray();
                var byKind = evidence.GroupBy(item => item.Kind).OrderBy(items => Array.IndexOf(Kinds, items.Key)).ToDictionary(items => items.Key, items => items.Count(), StringComparer.Ordinal);
                var limit = Math.Max(0, evidenceLimit);
                return new ImpliedDependency(
                    group.First().Source,
                    group.First().Target,
                    evidence.Select(item => (item.Consumer.Key, item.Producer.Key)).Distinct().Count(),
                    evidence.Length,
                    byKind,
                    DistinctNodes(evidence.Select(item => item.Consumer)),
                    DistinctNodes(evidence.Select(item => item.Producer)),
                    [.. evidence.Take(limit)],
                    evidence.Length,
                    evidence.Length > limit);
            })];
    }

    /// <summary>
    /// Finds mutual dependencies at a single model level using only ordering kinds.
    /// </summary>
    /// <param name="level">The module, feature or slice level; mixed levels have no cycle view.</param>
    /// <param name="kinds">Optional subset of ordering kinds.</param>
    /// <returns>Cycle members and groups in authored order.</returns>
    /// <exception cref="InvalidDependencyQuery">The level or kind is unsupported.</exception>
    public IReadOnlyList<DependencyGroup> Cycles(string level, IEnumerable<string>? kinds = null)
    {
        if (level != "slice" && level != "feature" && level != "module") throw new InvalidDependencyQuery($"Unsupported cycle level '{level}'.");
        var edges = Implied(level, level, OrderingKinds(kinds));
        var nodes = DistinctNodes(edges.SelectMany(edge => new[] { edge.Source, edge.Target }));
        var graph = Adjacency(nodes, edges.Select(edge => (edge.Source, edge.Target)));

        return Groups(graph, nodes, null);
    }

    /// <summary>
    /// Projects dependencies onto immediate siblings at their lowest common container.
    /// </summary>
    /// <param name="kinds">Optional subset of ordering kinds.</param>
    /// <returns>Mutual sibling groups in container and member authored order.</returns>
    /// <exception cref="InvalidDependencyQuery">A kind is unsupported.</exception>
    public IReadOnlyList<DependencyGroup> SiblingGroups(IEnumerable<string>? kinds = null)
    {
        var edges = SiblingEdges(kinds);

        return [.. Containers().SelectMany(container => Groups(Adjacency(_children[container.Key], edges.Where(edge => edge.Container == container).Select(edge => (edge.Source, edge.Target))), _children[container.Key], container))];
    }

    /// <summary>
    /// Suggests producers-first child order via Kahn's algorithm on condensed sibling groups.
    /// </summary>
    /// <param name="kinds">Optional subset of ordering kinds.</param>
    /// <returns>Per-container suggestions and a depth-first story traversal; no edit is performed.</returns>
    /// <exception cref="InvalidDependencyQuery">A kind is unsupported.</exception>
    public DependencyOrder SuggestedOrder(IEnumerable<string>? kinds = null)
    {
        var edges = SiblingEdges(kinds);
        var suggestions = new List<DependencyContainerOrder>();
        foreach (var container in Containers())
        {
            var children = _children[container.Key];
            var local = edges.Where(edge => edge.Container == container).ToArray();
            var graph = Adjacency(children, local.Select(edge => (edge.Source, edge.Target)));
            var byKey = children.ToDictionary(node => node.Key, StringComparer.Ordinal);
            var groups = StronglyConnectedGroups.In(graph).Select(group => group.Select(key => byKey[key]).OrderBy(node => node.Rank).ToArray()).OrderBy(group => group[0].Rank).ToArray();
            var membership = groups.SelectMany((group, index) => group.Select(node => (node.Key, Index: index))).ToDictionary(item => item.Key, item => item.Index, StringComparer.Ordinal);
            var dependencies = children.ToDictionary(node => node.Key, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (var edge in local)
            {
                if (membership[edge.Source.Key] != membership[edge.Target.Key]) dependencies[edge.Source.Key].Add(edge.Target.Key);
            }

            // Remove internal cycle edges, but retain the members' relative authored order.
            // A group need not become contiguous and displace unrelated children.
            foreach (var group in groups)
            {
                for (var index = 1; index < group.Length; index++) dependencies[group[index].Key].Add(group[index - 1].Key);
            }
            var remaining = children.Select(node => node.Key).ToHashSet(StringComparer.Ordinal);
            var ordered = new List<DependencyNode>();
            while (remaining.Count > 0)
            {
                var ready = remaining.Where(key => dependencies[key].Count == 0).OrderBy(key => byKey[key].Rank).ThenBy(key => key, StringComparer.Ordinal).First();
                ordered.Add(byKey[ready]);
                remaining.Remove(ready);
                foreach (var key in remaining) dependencies[key].Remove(ready);
            }
            suggestions.Add(new(container, ordered, !children.SequenceEqual(ordered)));
        }
        var orders = suggestions.ToDictionary(item => item.Container.Key, item => item.Children, StringComparer.Ordinal);
        var slices = new List<DependencyNode>();
        void Walk(DependencyNode node)
        {
            if (node.Kind == "slice") slices.Add(node);
            else foreach (var child in orders[node.Key].OrderBy(child => child.Kind != "slice")) Walk(child);
        }
        Walk(_root);

        return new(suggestions, slices);
    }

    /// <summary>
    /// Traverses slice dependencies transitively, in either direction, returning each reached node once.
    /// </summary>
    /// <param name="address">The starting slice or context address.</param>
    /// <param name="direction">incoming for consumers, outgoing for producers.</param>
    /// <param name="kinds">Optional dependency kinds.</param>
    /// <param name="includeTestOnly">Whether to traverse specification references.</param>
    /// <returns>Reached nodes in authored order, excluding the starting node.</returns>
    /// <exception cref="InvalidDependencyQuery">The direction or kind is unsupported.</exception>
    public IReadOnlyList<DependencyNode> Traverse(string address, string direction, IEnumerable<string>? kinds = null, bool includeTestOnly = false)
    {
        if (direction != "incoming" && direction != "outgoing") throw new InvalidDependencyQuery($"Unsupported dependency direction '{direction}'.");
        var links = Evidence(kinds, includeTestOnly).Select(item => direction == "incoming" ? (Source: item.Producer, Target: item.Consumer) : (Source: item.Consumer, Target: item.Producer)).ToArray();
        var visited = new HashSet<string>(StringComparer.Ordinal) { address };
        var queue = new Queue<string>();
        var nodes = new List<DependencyNode>();
        queue.Enqueue(address);
        while (queue.TryDequeue(out var current))
        {
            foreach (var link in links.Where(link => link.Source.Address == current).OrderBy(link => link.Target.Rank))
            {
                if (!visited.Add(link.Target.Address)) continue;
                nodes.Add(link.Target);
                queue.Enqueue(link.Target.Address);
            }
        }

        return DistinctNodes(nodes);
    }

    static IReadOnlyList<DependencyNode> DistinctNodes(IEnumerable<DependencyNode> nodes) => [.. nodes.DistinctBy(node => node.Key).OrderBy(node => node.Rank).ThenBy(node => node.Key, StringComparer.Ordinal)];
    static IEnumerable<string> OrderingKinds(IEnumerable<string>? kinds) => SelectedKinds(kinds ?? _orderingKinds).Where(kind => _orderingKinds.Contains(kind, StringComparer.Ordinal));

    static HashSet<string> SelectedKinds(IEnumerable<string>? kinds)
    {
        var selected = (kinds ?? Kinds).ToHashSet(StringComparer.Ordinal);
        if (selected.Any(kind => !Kinds.Contains(kind, StringComparer.Ordinal))) throw new InvalidDependencyQuery("Unsupported dependency kind.");

        return selected;
    }

    static Dictionary<string, HashSet<string>> Adjacency(IEnumerable<DependencyNode> nodes, IEnumerable<(DependencyNode Source, DependencyNode Target)> edges)
    {
        var graph = nodes.ToDictionary(node => node.Key, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var (source, target) in edges) graph[source.Key].Add(target.Key);

        return graph;
    }

    static IReadOnlyList<DependencyGroup> Groups(IReadOnlyDictionary<string, HashSet<string>> graph, IEnumerable<DependencyNode> nodes, DependencyNode? container)
    {
        var byKey = nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);

        return [.. StronglyConnectedGroups.In(graph).Where(group => group.Count > 1).Select(group => new DependencyGroup(container, DistinctNodes(group.Select(key => byKey[key])))).OrderBy(group => group.Members[0].Rank)];
    }

    internal bool IsWithin(DependencyNode node, DependencyNode scope) => node.Key == scope.Key || Ancestors(node).Any(ancestor => ancestor.Key == scope.Key);

    bool Disjoint(DependencyNode left, DependencyNode right) => left.Key != right.Key && !Ancestors(left).Contains(right) && !Ancestors(right).Contains(left);

    IEnumerable<DependencyEvidence> Evidence(IEnumerable<string>? kinds, bool includeTestOnly)
    {
        var selected = SelectedKinds(kinds);

        return Edges.SelectMany(edge => edge.Evidence).Where(item => selected.Contains(item.Kind) && (includeTestOnly || !item.TestOnly));
    }

    IEnumerable<DependencyNode> At(DependencyNode slice, string level) => Ancestors(slice).Prepend(slice).Where(node => node.Kind == level);

    IEnumerable<DependencyNode> Containers() => new[] { _root }.Concat(_nodes.Where(node => node.Kind == "module" || node.Kind == "feature"));

    List<SiblingEdge> SiblingEdges(IEnumerable<string>? kinds)
    {
        var result = new List<SiblingEdge>();
        foreach (var item in Evidence(OrderingKinds(kinds), false))
        {
            var consumer = Path(item.Consumer);
            var producer = Path(item.Producer);
            var common = 0;
            while (common < consumer.Count && common < producer.Count && consumer[common].Key == producer[common].Key) common++;
            if (common == consumer.Count || common == producer.Count) continue;
            result.Add(new(common == 0 ? _root : consumer[common - 1], consumer[common], producer[common]));
        }

        return result;
    }

    IReadOnlyList<DependencyNode> Path(DependencyNode slice) => [.. Ancestors(slice).Reverse().Append(slice).Where(node => node != _root)];

    IEnumerable<DependencyNode> Ancestors(DependencyNode node)
    {
        while (_parents.TryGetValue(node.Key, out var parent))
        {
            yield return parent;
            node = parent;
        }
    }

    sealed record SiblingEdge(DependencyNode Container, DependencyNode Source, DependencyNode Target);
}
