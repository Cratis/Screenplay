// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Evaluates graph queries over the reference runner's in-memory graph. A sink must give the same rows for the same graph.
/// </summary>
internal static class SemanticGraphQueryRunner
{
    internal static IEnumerable<SemanticSlice> SlicesOf(SemanticModule module) => module.Features.SelectMany(SlicesOf);

    internal static void Assert(
        SemanticApplication application,
        Dictionary<SemanticId, SemanticReadModel> readModels,
        SemanticGraphRunner.GraphSnapshot snapshot,
        SemanticGraphQueryExpectation expectation,
        ImmutableArray<string>.Builder failures)
    {
        var query = application.GraphQueries.FirstOrDefault(candidate => candidate.Name == expectation.Query);
        if (query is null)
        {
            failures.Add($"Expected graph query '{expectation.Query}', but the model declares none by that name.");
            return;
        }

        var readModel = readModels[query.ReadModel];
        var arguments = expectation.Arguments.ToDictionary(argument => argument.Property, argument => SemanticGraphRunner.Canonical(argument.Value), StringComparer.Ordinal);
        var collecting = !query.Collects.IsDefaultOrEmpty;
        var actual = collecting ? Collect(application, snapshot, query, readModel, arguments) : Run(application, snapshot, query, readModel, arguments);
        var remaining = actual.ToList();
        foreach (var expected in expectation.Rows)
        {
            var index = collecting
                ? remaining.FindIndex(row => expected.Values.All(value => row.TryGetValue(value.Property, out var held) && Same(value.Value, held, expectation.Exactly)) &&
                    (!expectation.Exactly || row.Where(entry => entry.Value is not SemanticNullValue).All(entry => expected.Values.Any(value => value.Property == entry.Key))))
                : remaining.FindIndex(row => expected.Values.All(value =>
                row.TryGetValue(value.Property, out var held) && SemanticGraphRunner.Canonical(held) == SemanticGraphRunner.Canonical(value.Value)) &&
                (!expectation.Exactly || row.Keys.All(key => expected.Values.Any(value => value.Property == key))));
            if (index < 0)
            {
                failures.Add($"Expected graph query '{query.Name}' to return a row {(collecting ? DescribeCollected(expected) : Describe(expected))}, but no row matches.");
            }
            else
            {
                remaining.RemoveAt(index);
            }
        }

        if (remaining.Count > 0)
        {
            failures.Add($"Expected graph query '{query.Name}' to return {expectation.Rows.Length} row(s), but it returned {actual.Count}.");
        }
    }

    /// <summary>
    /// Evaluates a graph query that collects nested rows, as a graph view reads (ESM v54): one row per root node, distinct by key and ordered by key;
    /// conditions select roots, a condition on a collected node requiring at least one reached node to satisfy all conditions on it; every collect
    /// assigns the full set of rows reached along its edge, ordered by the reached node's key and then by the edge's identity.
    /// </summary>
    /// <param name="application">The application the query belongs to.</param>
    /// <param name="snapshot">The graph to read.</param>
    /// <param name="query">The query, which collects.</param>
    /// <param name="readModel">The read model the rows are returned as.</param>
    /// <param name="arguments">The canonical argument values by name.</param>
    /// <returns>The rows, read-model property names to values; a list query returns every root, a single query the first root or none.</returns>
    internal static List<Dictionary<string, SemanticValue>> Collect(
        SemanticApplication application,
        SemanticGraphRunner.GraphSnapshot snapshot,
        SemanticGraphQuery query,
        SemanticReadModel readModel,
        Dictionary<string, string?> arguments)
    {
        var graph = application.Graphs.First(candidate => candidate.Name == query.Graph);
        var root = query.Matches[0].Start;
        var collectedNodes = query.Collects.Where(collect => collect.Hop.Node.Alias is not null).ToDictionary(collect => collect.Hop.Node.Alias!, StringComparer.Ordinal);
        var rows = new List<Dictionary<string, SemanticValue>>();
        foreach (var (key, properties) in snapshot.Nodes(graph.Name, root.Label).OrderBy(node => node.Key, KeyComparer.Instance))
        {
            var selected = query.Conditions.Where(condition => condition.Alias == root.Alias).All(condition => Holds(properties, condition, arguments)) &&
                query.Conditions.Where(condition => condition.Alias != root.Alias).GroupBy(condition => condition.Alias, StringComparer.Ordinal).All(group =>
                    collectedNodes.TryGetValue(group.Key, out var collect) &&
                    Reached(snapshot, graph, key, collect).Any(reached => group.All(condition => Holds(reached.Node, condition, arguments))));
            if (!selected) continue;

            var row = new Dictionary<string, SemanticValue>(StringComparer.Ordinal);
            foreach (var @return in query.Returns)
            {
                row[readModel.Properties.First(property => property.Id == @return.Property).Name] = properties.TryGetValue(@return.Source, out var value) ? value : SemanticValue.Null;
            }

            foreach (var collect in query.Collects)
            {
                var items = Reached(snapshot, graph, key, collect).Select(reached => SemanticValue.Composite([.. collect.Members.Select(member =>
                    new SemanticPropertyValue(member.Property, (member.Kind == SemanticGraphCollectMemberKind.Property ? reached.Node : reached.Edge).TryGetValue(member.Source, out var value) ? value : SemanticValue.Null))])).ToList();
                var name = readModel.Properties.First(property => property.Id == collect.Property).Name;
                row[name] = collect.Cardinality == SemanticGraphCollectCardinality.Many ? SemanticValue.Array([.. items]) : items.FirstOrDefault() ?? SemanticValue.Null;
            }

            rows.Add(row);
            if (query.Cardinality != SemanticQueryCardinality.Many) break;
        }

        return rows;
    }

    // The nodes reached from a root along a collect's edge, with the edge followed: ordered by the reached node's key, then the edge's identity.
    static IEnumerable<(Dictionary<string, SemanticValue> Node, Dictionary<string, SemanticValue> Edge)> Reached(SemanticGraphRunner.GraphSnapshot snapshot, SemanticGraph graph, string root, SemanticGraphCollect collect)
    {
        var nodes = snapshot.Nodes(graph.Name, collect.Hop.Node.Label).ToDictionary(node => node.Key, node => node.Properties, StringComparer.Ordinal);
        return snapshot.IdentifiedEdges(graph.Name, collect.Hop.Type)
            .Select(edge => (Near: collect.Hop.Outgoing ? edge.Source : edge.Target, Far: collect.Hop.Outgoing ? edge.Target : edge.Source, edge.Identity, edge.Properties))
            .Where(edge => edge.Near == root && nodes.ContainsKey(edge.Far))
            .OrderBy(edge => edge.Far, KeyComparer.Instance)
            .ThenBy(edge => edge.Identity, StringComparer.Ordinal)
            .Select(edge => (nodes[edge.Far], edge.Properties));
    }

    static bool Holds(Dictionary<string, SemanticValue> element, SemanticGraphCondition condition, Dictionary<string, string?> arguments)
    {
        if (!element.TryGetValue(condition.Property, out var held)) return false;
        var expected = condition.Argument is not null ? arguments.GetValueOrDefault(condition.Argument) : SemanticGraphRunner.Canonical(condition.Literal);
        return SemanticGraphRunner.Canonical(held) == expected;
    }

    // Compares an expected value with what a collecting query returned: lists in order and of equal length, composites by the members the
    // expectation states (and, when exact, with every other member null), scalars by their canonical text.
    static bool Same(SemanticValue expected, SemanticValue? actual, bool exactly) => (expected, actual) switch
    {
        (SemanticArrayValue list, SemanticArrayValue held) => list.Values.Length == held.Values.Length && list.Values.Zip(held.Values).All(pair => Same(pair.First, pair.Second, exactly)),
        (SemanticCompositeValue composite, SemanticCompositeValue held) =>
            composite.Properties.All(member => Same(member.Value, held.Properties.FirstOrDefault(candidate => candidate.TargetProperty == member.TargetProperty)?.Value ?? SemanticValue.Null, exactly)) &&
            (!exactly || held.Properties.Where(member => member.Value is not SemanticNullValue).All(member => composite.Properties.Any(candidate => candidate.TargetProperty == member.TargetProperty))),
        (SemanticNullValue, null or SemanticNullValue) => true,
        (SemanticArrayValue or SemanticCompositeValue, _) => false,
        _ => actual is not null and not SemanticArrayValue and not SemanticCompositeValue && SemanticGraphRunner.Canonical(expected) == SemanticGraphRunner.Canonical(actual)
    };

    static IEnumerable<SemanticSlice> SlicesOf(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(SlicesOf));

    static string DescribeCollected(SemanticGraphRow row) => "{ " + string.Join(", ", row.Values.Select(value => $"{value.Property} = {Render(value.Value)}")) + " }";

    static string? Render(SemanticValue value) => value switch
    {
        SemanticArrayValue list => "[" + string.Join(", ", list.Values.Select(Render)) + "]",
        SemanticCompositeValue composite => "{ " + string.Join(", ", composite.Properties.Select(member => Render(member.Value))) + " }",
        _ => SemanticGraphRunner.Canonical(value)
    };

    static string Describe(SemanticGraphRow row) => "{ " + string.Join(", ", row.Values.Select(value => $"{value.Property} = {SemanticGraphRunner.Canonical(value.Value)}")) + " }";

    static List<Dictionary<string, SemanticValue>> Run(
        SemanticApplication application,
        SemanticGraphRunner.GraphSnapshot snapshot,
        SemanticGraphQuery query,
        SemanticReadModel readModel,
        Dictionary<string, string?> arguments)
    {
        var graph = application.Graphs.First(candidate => candidate.Name == query.Graph);
        var bindings = new List<Binding> { Binding.Empty }.AsEnumerable();
        foreach (var match in query.Matches) bindings = Extend(bindings, snapshot, graph, match);
        var rows = new List<Dictionary<string, SemanticValue>>();
        foreach (var binding in bindings)
        {
            if (!query.Conditions.All(condition => Satisfies(binding, condition, arguments))) continue;
            var row = new Dictionary<string, SemanticValue>(StringComparer.Ordinal);
            foreach (var @return in query.Returns)
            {
                if (binding.Elements.TryGetValue(@return.Alias, out var element) && element is not null && element.TryGetValue(@return.Source, out var value))
                {
                    row[readModel.Properties.First(property => property.Id == @return.Property).Name] = value;
                }
            }

            rows.Add(row);
        }

        return rows;
    }

    static IEnumerable<Binding> Extend(IEnumerable<Binding> bindings, SemanticGraphRunner.GraphSnapshot snapshot, SemanticGraph graph, SemanticGraphMatch match)
    {
        foreach (var binding in bindings)
        {
            var extended = Paths(binding, snapshot, graph, match).ToList();
            if (extended.Count > 0)
            {
                foreach (var path in extended) yield return path;
            }
            else if (match.IsOptional)
            {
                yield return binding;
            }
        }
    }

    static IEnumerable<Binding> Paths(Binding binding, SemanticGraphRunner.GraphSnapshot snapshot, SemanticGraph graph, SemanticGraphMatch match)
    {
        var current = Starts(binding, snapshot, graph, match.Start);
        foreach (var hop in match.Hops)
        {
            current = current.SelectMany(step => Hop(step.Binding, step.Key, snapshot, graph, hop, match));
        }

        return current.Select(step => step.Binding);
    }

    static IEnumerable<(Binding Binding, string Key)> Starts(Binding binding, SemanticGraphRunner.GraphSnapshot snapshot, SemanticGraph graph, SemanticGraphPatternNode start)
    {
        if (start.Alias is not null && binding.Nodes.TryGetValue(start.Alias, out var bound))
        {
            if (bound.Key is not null) yield return (binding, bound.Key);
            yield break;
        }

        foreach (var (key, properties) in snapshot.Nodes(graph.Name, start.Label))
        {
            yield return (Bind(binding, start, key, properties), key);
        }
    }

    static IEnumerable<(Binding Binding, string Key)> Hop(Binding binding, string from, SemanticGraphRunner.GraphSnapshot snapshot, SemanticGraph graph, SemanticGraphHop hop, SemanticGraphMatch match)
    {
        foreach (var (source, target, properties) in snapshot.Edges(graph.Name, hop.Type))
        {
            var (near, far) = hop.Outgoing ? (source, target) : (target, source);
            if (near != from) continue;
            var node = snapshot.Nodes(graph.Name, hop.Node.Label).FirstOrDefault(candidate => candidate.Key == far);
            if (node.Properties is null) continue;
            if (hop.Node.Alias is not null && binding.Nodes.TryGetValue(hop.Node.Alias, out var bound) && bound.Key != far) continue;
            var next = Bind(binding, hop.Node, far, node.Properties);
            if (hop.Alias is not null) next = next with { Elements = next.Elements.SetItem(hop.Alias, properties) };
            yield return (next, far);
        }
    }

    static Binding Bind(Binding binding, SemanticGraphPatternNode node, string key, Dictionary<string, SemanticValue> properties) =>
        node.Alias is null
            ? binding
            : binding with { Elements = binding.Elements.SetItem(node.Alias, properties), Nodes = binding.Nodes.SetItem(node.Alias, (node.Label, key)) };

    static bool Satisfies(Binding binding, SemanticGraphCondition condition, Dictionary<string, string?> arguments)
    {
        if (!binding.Elements.TryGetValue(condition.Alias, out var element) || element is null || !element.TryGetValue(condition.Property, out var held)) return false;
        var expected = condition.Argument is not null ? arguments.GetValueOrDefault(condition.Argument) : SemanticGraphRunner.Canonical(condition.Literal);
        return SemanticGraphRunner.Canonical(held) == expected;
    }

    // Keys order numerically when both are numbers, otherwise ordinally - a Guid key orders by its string form.
    sealed class KeyComparer : IComparer<string>
    {
        public static readonly KeyComparer Instance = new();

        public int Compare(string? x, string? y) =>
            decimal.TryParse(x, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var left) &&
            decimal.TryParse(y, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var right)
                ? left.CompareTo(right)
                : string.CompareOrdinal(x, y);
    }

    // One path binding: the node and edge elements the aliases stand for, with the key of each node so a hop can continue from it.
    sealed record Binding(ImmutableDictionary<string, Dictionary<string, SemanticValue>?> Elements, ImmutableDictionary<string, (string Label, string? Key)> Nodes)
    {
        // net8 and net9 have no collection expression for an immutable dictionary, so the empty binding is built once, explicitly.
#pragma warning disable IDE0301
        public static readonly Binding Empty = new(ImmutableDictionary<string, Dictionary<string, SemanticValue>?>.Empty, ImmutableDictionary<string, (string Label, string? Key)>.Empty);
#pragma warning restore IDE0301
    }
}
