// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Infers presentation dependencies from explicit syntax, independently of executable binding and identity.
/// </summary>
public sealed partial class DependencyGraph
{
    internal static readonly string[] Kinds = ["usesFactsFrom", "reactsTo", "decidesFrom", "asks", "shows", "verifiedWith", "outsideTheModel"];
    static readonly string[] _orderingKinds = ["usesFactsFrom", "reactsTo", "decidesFrom"];
    readonly List<DependencyNode> _nodes = [];
    readonly List<(DependencyNode Node, SliceSyntax Syntax)> _slices = [];
    readonly Dictionary<string, List<DependencyNode>> _children = new(StringComparer.Ordinal);
    readonly DependencyNode _root = new("application", string.Empty, [], -1);

    DependencyGraph(ApplicationSyntax application, IReadOnlyDictionary<string, int>? ranks)
    {
        OrderSource = ranks is { Count: > 0 } ? "authored" : "syntax";
        ranks ??= new Dictionary<string, int>();
        _children[_root.Key] = [];
        void Feature(FeatureSyntax feature, DependencyNode parent)
        {
            var node = Add("feature", feature.Name, parent);
            foreach (var slice in Ordered(feature.Slices, node, slice => slice.Name, ranks))
            {
                _slices.Add((Add("slice", slice.Name, node), slice));
            }
            foreach (var child in Ordered(feature.Features, node, child => child.Name, ranks)) Feature(child, node);
        }
        foreach (var module in Ordered(application.Modules, _root, module => module.Name, ranks))
        {
            var node = Add("module", module.Name, _root);
            foreach (var feature in Ordered(module.Features, node, feature => feature.Name, ranks)) Feature(feature, node);
        }

        var declarations = new Dictionary<(string Kind, string Name), List<DependencyNode>>(new DeclarationNames());
        var builders = new Dictionary<(string Kind, string Name), List<DependencyNode>>(new DeclarationNames());
        void Declare(Dictionary<(string Kind, string Name), List<DependencyNode>> inventory, string kind, string name, DependencyNode node)
        {
            if (!inventory.TryGetValue((kind, name), out var owners)) inventory[(kind, name)] = owners = [];
            if (!owners.Contains(node)) owners.Add(node);
        }
        foreach (var (node, slice) in _slices)
        {
            foreach (var value in EventDeclarations.In(slice)) Declare(declarations, "Event", value.Name, node);
            foreach (var value in slice.Commands) Declare(declarations, "Command", value.Name, node);
            foreach (var value in slice.Queries) Declare(declarations, "Query", value.Name, node);
            foreach (var value in slice.Screens) Declare(declarations, "Screen", value.Name, node);
            foreach (var value in slice.ReadModels ?? []) Declare(declarations, "ReadModel", value.Name, node);
            foreach (var projection in slice.Projections)
            {
                var variants = Variants(projection.Blocks).ToArray();
                if (variants.Length == 0) Declare(builders, "ReadModel", projection.ReadModel ?? projection.Name, node);
                foreach (var variant in variants) Declare(builders, "ReadModel", variant.Name, node);
            }
            foreach (var reducer in slice.Reducers ?? []) Declare(builders, "ReadModel", reducer.ReadModel, node);
        }

        var foundations = (application.Triggers ?? []).Select(trigger => trigger.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedTypes = application.Concepts.Select(value => value.Name).Concat((application.Types ?? []).Select(value => value.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedPolicies = application.Policies.Select(value => value.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var imports = application.Imports.Where(import => import.QualifiedName.Contains('.', StringComparison.Ordinal))
            .OrderBy(import => import.Location.Path, StringComparer.Ordinal).ThenBy(import => import.Location.Line).ThenBy(import => import.Location.Column)
            .ThenBy(import => import.QualifiedName, StringComparer.Ordinal).ToArray();
        var contexts = new Dictionary<string, DependencyNode>(StringComparer.Ordinal);
        var evidence = new List<DependencyEvidence>();
        var unresolved = new List<UnresolvedDependency>();
        var usedImports = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (node, slice) in _slices)
        {
            var references = SliceReferences.In(slice, out var shared);
            ExcludedReferences += shared.Count(reference => (reference.Kind == "type" ? sharedTypes : sharedPolicies).Contains(reference.Name));
            foreach (var reference in references)
            {
                var inventory = reference.TargetKind == "ReadModel" && builders.ContainsKey((reference.TargetKind, reference.Name)) ? builders : declarations;
                inventory.TryGetValue((reference.TargetKind, reference.Name), out var candidates);
                if (candidates is { Count: > 0 })
                {
                    var producer = candidates[0];
                    if (producer != node) evidence.Add(new(node, producer, reference.Kind, reference.Role, reference.Name, candidates.Count > 1, [.. candidates.Skip(1)], reference.Location));
                    continue;
                }
                if (reference.Role == "trigger" && foundations.Contains(reference.Name))
                {
                    ExcludedReferences++;
                    continue;
                }
                var imported = reference.TargetKind == "Event" ? imports.FirstOrDefault(import => string.Equals(import.Name, reference.Name, StringComparison.OrdinalIgnoreCase)) : null;
                if (imported is not null)
                {
                    usedImports.Add(imported.QualifiedName);
                    var address = "context:" + imported.QualifiedName[..imported.QualifiedName.LastIndexOf('.')];
                    if (!contexts.TryGetValue(address, out var context))
                    {
                        context = new("context", address, [address[8..]], _nodes.Count);
                        _nodes.Add(context);
                        contexts[address] = context;
                    }
                    evidence.Add(new(node, context, "outsideTheModel", reference.Role, reference.Name, false, [], reference.Location) { TestOnly = reference.Kind == "verifiedWith" });
                    continue;
                }
                unresolved.Add(new(node, reference.Kind, reference.Role, reference.Name, reference.Location));
            }
        }
        Edges = [.. OrderedEvidence(evidence).GroupBy(item => (item.Consumer.Key, item.Producer.Key, item.Kind))
            .Select(group => new DependencyEdge(group.First().Consumer, group.First().Producer, group.Key.Kind, [.. group]))];
        Unresolved = [.. unresolved.OrderBy(item => item.Consumer.Rank).ThenBy(item => item.Location.Line).ThenBy(item => item.Location.Column)
            .ThenBy(item => Array.IndexOf(Kinds, item.Kind)).ThenBy(item => item.Location.Path, StringComparer.Ordinal).ThenBy(item => item.Name, StringComparer.Ordinal)];
        UnusedImports = [.. imports.Where(import => !usedImports.Contains(import.QualifiedName)).Select(import => import.QualifiedName).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets whether ranks came from authored import order or syntax fallback.
    /// </summary>
    public string OrderSource { get; }

    /// <summary>
    /// Gets all slice dependencies including test-only and outside-context edges.
    /// </summary>
    public IReadOnlyList<DependencyEdge> Edges { get; }

    /// <summary>
    /// Gets explicit references that did not resolve; they never become edges.
    /// </summary>
    public IReadOnlyList<UnresolvedDependency> Unresolved { get; }

    /// <summary>
    /// Gets the count of references to application-level shared foundations.
    /// </summary>
    public int ExcludedReferences { get; }

    /// <summary>
    /// Gets contracts with no outside reference (including locally satisfied imports).
    /// </summary>
    public IReadOnlyList<string> UnusedImports { get; }

    /// <summary>
    /// Gets model and context nodes in presentation order.
    /// </summary>
    public IReadOnlyList<DependencyNode> Nodes => _nodes.AsReadOnly();

    /// <summary>
    /// Infers the graph from merged syntax with optional authored presentation ranks.
    /// </summary>
    /// <param name="application">The application syntax; no executable admission is required.</param>
    /// <param name="ranks">Optional authored ranks keyed by JSON arrays of scope segments.</param>
    /// <returns>The inferred graph.</returns>
    public static DependencyGraph For(ApplicationSyntax application, IReadOnlyDictionary<string, int>? ranks = null) => new(application, ranks);

    internal static DependencyGraph For(AuthoredTimeline timeline) => new(timeline.Application ?? new ApplicationSyntax([], [], [], [], Diagnostics.SourceLocation.Start), timeline.Ranks);

    static IEnumerable<T> Ordered<T>(IEnumerable<T> values, DependencyNode parent, Func<T, string> name, IReadOnlyDictionary<string, int> ranks) =>
        values.Select((value, index) => (Value: value, Index: index, Rank: ranks.GetValueOrDefault(AuthoredOrder.Key(parent.Scope.Append(name(value))), int.MaxValue)))
            .OrderBy(item => item.Rank).ThenBy(item => item.Index).Select(item => item.Value);

    static IEnumerable<ProjectionVariantSyntax> Variants(IEnumerable<ProjectionBlockSyntax> blocks) => blocks.SelectMany(block => block switch
    {
        ProjectionVariantSyntax variant => new[] { variant }.Concat(Variants(variant.Blocks)),
        ChildrenSyntax children => Variants(children.Blocks),
        NestedSyntax nested => Variants(nested.Blocks),
        _ => []
    });

    static IOrderedEnumerable<DependencyEvidence> OrderedEvidence(IEnumerable<DependencyEvidence> evidence) => evidence.OrderBy(item => item.Consumer.Rank)
        .ThenBy(item => item.Location.Line).ThenBy(item => item.Location.Column).ThenBy(item => Array.IndexOf(Kinds, item.Kind))
        .ThenBy(item => item.Producer.Rank).ThenBy(item => item.Location.Path, StringComparer.Ordinal).ThenBy(item => item.Role, StringComparer.Ordinal).ThenBy(item => item.Name, StringComparer.Ordinal);

    DependencyNode Add(string kind, string name, DependencyNode parent)
    {
        string[] scope = [.. parent.Scope, name];
        var node = new DependencyNode(kind, string.Join('.', scope), scope, _nodes.Count);
        _nodes.Add(node);
        _children[parent.Key].Add(node);
        if (kind != "slice") _children[node.Key] = [];

        return node;
    }

    sealed class DeclarationNames : IEqualityComparer<(string Kind, string Name)>
    {
        public bool Equals((string Kind, string Name) x, (string Kind, string Name) y) => x.Kind == y.Kind && StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
        public int GetHashCode((string Kind, string Name) obj) => HashCode.Combine(obj.Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
    }
}
