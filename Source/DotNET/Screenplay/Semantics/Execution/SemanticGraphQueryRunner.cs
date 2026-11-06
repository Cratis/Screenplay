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
        var actual = Run(application, snapshot, query, readModel, arguments);
        var remaining = actual.ToList();
        foreach (var expected in expectation.Rows)
        {
            var index = remaining.FindIndex(row => expected.Values.All(value =>
                row.TryGetValue(value.Property, out var held) && SemanticGraphRunner.Canonical(held) == SemanticGraphRunner.Canonical(value.Value)) &&
                (!expectation.Exactly || row.Keys.All(key => expected.Values.Any(value => value.Property == key))));
            if (index < 0)
            {
                failures.Add($"Expected graph query '{query.Name}' to return a row {Describe(expected)}, but no row matches.");
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

    static IEnumerable<SemanticSlice> SlicesOf(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(SlicesOf));

    static string Describe(SemanticGraphRow row) => "{ " + string.Join(", ", row.Values.Select(value => $"{value.Property} = {SemanticGraphRunner.Canonical(value.Value)}")) + " }";

    static List<Dictionary<string, SemanticValue>> Run(
        SemanticApplication application,
        SemanticGraphRunner.GraphSnapshot snapshot,
        SemanticGraphQuery query,
        SemanticReadModel readModel,
        Dictionary<string, string?> arguments)
    {
        var graph = application.Graphs.First(candidate => candidate.Name == query.Graph);
        var bindings = new List<Binding> { new([], []) }.AsEnumerable();
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

    // One path binding: the node and edge elements the aliases stand for, with the key of each node so a hop can continue from it.
    sealed record Binding(ImmutableDictionary<string, Dictionary<string, SemanticValue>?> Elements, ImmutableDictionary<string, (string Label, string? Key)> Nodes);
}
