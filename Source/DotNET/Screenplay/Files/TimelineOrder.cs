// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Observes event flow on the presentation timeline without changing the model.
/// </summary>
internal static class TimelineOrder
{
    internal static IReadOnlyList<Diagnostic> In(ApplicationSyntax application, IReadOnlyDictionary<string, int>? order = null) =>
        [.. Analyze(application, order).Select(finding => finding.Diagnostic)];

    internal static IReadOnlyList<TimelineFinding> Analyze(ApplicationSyntax application, IReadOnlyDictionary<string, int>? order = null)
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
            foreach (var produced in EventDeclarations.In(slice.Syntax))
            {
                producers.TryAdd(produced.Name, slice);
            }
        }

        var edges = new List<Edge>();
        foreach (var consumer in slices)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = SliceReferences.In(consumer.Syntax).Where(reference => reference.Timeline)
                .Select(reference => new Reference(reference.Name, reference.Location))
                .OrderBy(reference => reference.Location.Line).ThenBy(reference => reference.Location.Column);
            foreach (var reference in references)
            {
                if (!seen.Add(reference.Event) || !producers.TryGetValue(reference.Event, out var producer) || producer == consumer)
                {
                    continue;
                }

                var common = 0;
                while (common < consumer.Scope.Length && common < producer.Scope.Length && consumer.Identity[common] == producer.Identity[common])
                {
                    common++;
                }

                if (common == consumer.Scope.Length || common == producer.Scope.Length)
                {
                    continue;
                }

                edges.Add(new(consumer, producer, reference.Event, reference.Location, AuthoredOrder.Key(consumer.Identity.Take(common)), consumer.Identity[common], producer.Identity[common]));
            }
        }

        var suppressed = new HashSet<Edge>();
        var findings = new List<(Edge Edge, TimelineFinding Finding)>();
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
                }).Where(end => end.Name == member).Min(end => end.Index)).ToArray();
                findings.Add((first, Finding(first, false, orderedMembers, new(
                    DiagnosticSeverity.Information,
                    DiagnosticCodes.TimelineCycleGroup,
                    $"Timeline group {string.Join(", ", orderedMembers.Select(member => $"'{member[(member.IndexOf(':') + 1)..]}'"))} uses each other's events; reordering these members cannot make every event flow left to right.",
                    first.Location))));
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
            findings.Add((edge, Finding(edge, ownSubFeature, [], new(
                DiagnosticSeverity.Information,
                DiagnosticCodes.EventFromLaterSlice,
                $"Slice '{edge.Consumer.Syntax.Name}' uses event '{edge.Event}' produced by slice '{edge.Producer.Syntax.Name}' drawn after it.{consequence}",
                edge.Location))));
        }

        return [.. findings.OrderBy(finding => finding.Edge.Consumer.Index).ThenBy(finding => finding.Edge.Location.Line).ThenBy(finding => finding.Edge.Location.Column).Select(finding => finding.Finding)];
    }

    static TimelineFinding Finding(Edge edge, bool ownSubFeature, string[] members, Diagnostic diagnostic) =>
        new(diagnostic, edge.Consumer.Scope, edge.Producer.Scope, edge.Event, edge.Container, edge.Left, edge.Right, ownSubFeature, members);

    static IEnumerable<T> Ordered<T>(IEnumerable<T> items, string[] outer, Func<T, string> name, IReadOnlyDictionary<string, int> order) =>
        items.Select(item => (Item: item, Rank: order.GetValueOrDefault(AuthoredOrder.Key(outer.Append(name(item))), int.MaxValue))).OrderBy(entry => entry.Rank).Select(entry => entry.Item);

    static IOrderedEnumerable<Edge> InOrder(IEnumerable<Edge> edges) => edges.OrderBy(edge => edge.Consumer.Index).ThenBy(edge => edge.Location.Line).ThenBy(edge => edge.Location.Column);

    static IReadOnlyList<IReadOnlyList<string>> StronglyConnected(IReadOnlyDictionary<string, HashSet<string>> graph) => StronglyConnectedGroups.In(graph);

    sealed record Slice(SliceSyntax Syntax, string[] Scope, int Index)
    {
        // Container children and slice children occupy separate groups even when their names match.
        public string[] Identity { get; } = [.. Scope.SkipLast(1).Select(name => $"container:{name}"), $"slice:{Syntax.Name}"];
    }
    sealed record Reference(string Event, SourceLocation Location);
    sealed record Edge(Slice Consumer, Slice Producer, string Event, SourceLocation Location, string Container, string Left, string Right);
}
