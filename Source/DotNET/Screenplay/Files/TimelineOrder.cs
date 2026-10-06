// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Observes event flow on the presentation timeline without changing the model.
/// </summary>
internal static class TimelineOrder
{
    internal static IReadOnlyList<Diagnostic> In(ApplicationSyntax application, IReadOnlyDictionary<string, int>? order = null)
    {
        order ??= new Dictionary<string, int>();
        var slices = new List<Slice>();
        void Feature(FeatureSyntax feature, string[] outer)
        {
            string[] scope = [.. outer, feature.Name];
            foreach (var slice in Ordered(feature.Slices, scope, slice => slice.Name, order))
            {
                slices.Add(new(slice, [.. scope, slice.Name], slices.Count));
            }

            foreach (var child in Ordered(feature.Features, scope, child => child.Name, order))
            {
                Feature(child, scope);
            }
        }

        foreach (var module in Ordered(application.Modules, [], module => module.Name, order))
        {
            foreach (var feature in Ordered(module.Features, [module.Name], feature => feature.Name, order))
            {
                Feature(feature, [module.Name]);
            }
        }

        var producers = new Dictionary<string, Slice>(StringComparer.OrdinalIgnoreCase);
        foreach (var slice in slices)
        {
            foreach (var produced in slice.Syntax.Events)
            {
                producers.TryAdd(produced.Name, slice);
            }
        }

        var edges = new List<Edge>();
        foreach (var consumer in slices)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = consumer.Syntax.Projections.SelectMany(projection => projection.Blocks.SelectMany(EventsOf))
                .Concat(consumer.Syntax.Reactions.SelectMany(reaction => reaction.Triggers).Where(trigger => trigger.Source is NamedTriggerSourceSyntax)
                    .Select(trigger => new Reference(((NamedTriggerSourceSyntax)trigger.Source).Name, trigger.Source.Location)))
                .OrderBy(reference => reference.Location.Line).ThenBy(reference => reference.Location.Column);
            foreach (var reference in references)
            {
                if (!seen.Add(reference.Event) || !producers.TryGetValue(reference.Event, out var producer) || producer == consumer)
                {
                    continue;
                }

                var common = 0;
                while (common < consumer.Scope.Length && common < producer.Scope.Length && consumer.Scope[common] == producer.Scope[common])
                {
                    common++;
                }

                if (common == consumer.Scope.Length || common == producer.Scope.Length)
                {
                    continue;
                }

                edges.Add(new(consumer, producer, reference.Event, reference.Location, AuthoredOrder.Key(consumer.Scope.Take(common)), consumer.Scope[common], producer.Scope[common]));
            }
        }

        var suppressed = new HashSet<Edge>();
        var findings = new List<(Edge Edge, Diagnostic Diagnostic)>();
        foreach (var local in edges.GroupBy(edge => edge.Container, StringComparer.Ordinal))
        {
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var edge in local)
            {
                if (!graph.TryGetValue(edge.Left, out var outgoing))
                {
                    outgoing = new(StringComparer.Ordinal);
                    graph[edge.Left] = outgoing;
                }

                if (!graph.ContainsKey(edge.Right))
                {
                    graph[edge.Right] = new(StringComparer.Ordinal);
                }

                outgoing.Add(edge.Right);
            }

            foreach (var group in StronglyConnected(graph).Where(group => group.Count > 1))
            {
                var members = group.ToHashSet(StringComparer.Ordinal);
                var internalEdges = local.Where(edge => members.Contains(edge.Left) && members.Contains(edge.Right)).ToArray();
                suppressed.UnionWith(internalEdges);
                var first = InOrder(internalEdges.Where(edge => edge.Consumer.Index < edge.Producer.Index)).FirstOrDefault();
                if (first is null)
                {
                    continue;
                }

                var orderedMembers = group.OrderBy(member => internalEdges.SelectMany(edge => new[]
                {
                    (Name: edge.Left, edge.Consumer.Index),
                    (Name: edge.Right, edge.Producer.Index)
                }).Where(end => end.Name == member).Min(end => end.Index));
                findings.Add((first, new(
                    DiagnosticSeverity.Information,
                    DiagnosticCodes.TimelineCycleGroup,
                    $"Timeline group {string.Join(", ", orderedMembers.Select(member => $"'{member}'"))} uses each other's events; reordering these members cannot make every event flow left to right.",
                    first.Location)));
            }
        }

        foreach (var edge in edges)
        {
            if (edge.Consumer.Index >= edge.Producer.Index || suppressed.Contains(edge))
            {
                continue;
            }

            var ownSubFeature = edge.Producer.Scope.Length > edge.Consumer.Scope.Length && edge.Consumer.Scope.SkipLast(1).Select((name, index) => name == edge.Producer.Scope[index]).All(same => same);
            var consequence = ownSubFeature ? " The producer is in the consumer's own sub-feature; this cannot be fixed by reordering." : " Consider drawing the producer before the consumer.";
            findings.Add((edge, new(
                DiagnosticSeverity.Information,
                DiagnosticCodes.EventFromLaterSlice,
                $"Slice '{edge.Consumer.Syntax.Name}' uses event '{edge.Event}' produced by slice '{edge.Producer.Syntax.Name}' drawn after it.{consequence}",
                edge.Location)));
        }

        return [.. findings.OrderBy(finding => finding.Edge.Consumer.Index).ThenBy(finding => finding.Edge.Location.Line).ThenBy(finding => finding.Edge.Location.Column).Select(finding => finding.Diagnostic)];
    }

    static IEnumerable<T> Ordered<T>(IEnumerable<T> items, string[] outer, Func<T, string> name, IReadOnlyDictionary<string, int> order) =>
        items.Select(item => (Item: item, Rank: order.GetValueOrDefault(AuthoredOrder.Key(outer.Append(name(item))), int.MaxValue))).OrderBy(entry => entry.Rank).Select(entry => entry.Item);

    static IOrderedEnumerable<Edge> InOrder(IEnumerable<Edge> edges) => edges.OrderBy(edge => edge.Consumer.Index).ThenBy(edge => edge.Location.Line).ThenBy(edge => edge.Location.Column);

    static IEnumerable<Reference> EventsOf(ProjectionBlockSyntax block) => block switch
    {
        FromSyntax from => from.Events.Select(value => new Reference(value.Event, value.Location)),
        JoinSyntax join => join.Events.Select(value => new Reference(value.Event, value.Location)),
        ChildrenSyntax children => children.Blocks.SelectMany(EventsOf),
        NestedSyntax nested => nested.Blocks.SelectMany(EventsOf),
        ProjectionVariantSyntax variant => variant.EntersOn.Select(value => new Reference(value.Event, value.Location)).Concat(variant.Blocks.SelectMany(EventsOf)),
        RemoveWithSyntax remove => [new(remove.Event, remove.Location)],
        RemoveViaJoinSyntax remove => [new(remove.Event, remove.Location)],
        ClearWithSyntax clear => [new(clear.Event, clear.Location)],
        _ => []
    };

    static List<IReadOnlyList<string>> StronglyConnected(IReadOnlyDictionary<string, HashSet<string>> graph)
    {
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var low = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var active = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<IReadOnlyList<string>>();
        void Visit(string node)
        {
            indices[node] = indices.Count;
            low[node] = indices[node];
            stack.Push(node);
            active.Add(node);
            foreach (var next in graph[node])
            {
                if (!indices.TryGetValue(next, out var nextIndex))
                {
                    Visit(next);
                    low[node] = Math.Min(low[node], low[next]);
                }
                else if (active.Contains(next))
                {
                    low[node] = Math.Min(low[node], nextIndex);
                }
            }

            if (low[node] != indices[node])
            {
                return;
            }

            var group = new List<string>();
            string member;
            do
            {
                member = stack.Pop();
                active.Remove(member);
                group.Add(member);
            }
            while (member != node);
            groups.Add(group);
        }

        foreach (var node in graph.Keys)
        {
            if (!indices.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return groups;
    }

    sealed record Slice(SliceSyntax Syntax, string[] Scope, int Index);
    sealed record Reference(string Event, SourceLocation Location);
    sealed record Edge(Slice Consumer, Slice Producer, string Event, SourceLocation Location, string Container, string Left, string Right);
}
