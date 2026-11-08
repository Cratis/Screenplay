// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Walks one property's or screen field's lineage over the property-link index into the hop tree of decision 0099:
// the subject, then where its value goes (downstream) and where it comes from (upstream), each depth-first.
internal sealed partial class McpLineageTrace
{
    const int HopBudget = 500;
    const int DepthBudget = 24;

    readonly McpSyntaxIndex _index;
    readonly WorkspacePropertyLinks _links;
    readonly Dictionary<SyntaxNode, McpDeclaration> _owners = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<McpDeclaration, int> _reached = new(ReferenceEqualityComparer.Instance);
    readonly List<McpLineageHop> _hops = [];
    readonly Lazy<ILookup<WorkspaceNodeHandle, WorkspacePropertyLink>> _mappingSources;
    readonly Lazy<ILookup<WorkspaceNodeHandle, WorkspacePropertyLink>> _mappingTargets;
    bool _stopped;

    internal McpLineageTrace(McpSyntaxIndex index)
    {
        _index = index;
        _links = index.PropertyLinks;
        _mappingSources = new(() => _links.Links.Where(link => link.Reference.Role == "mapping-source").ToLookup(link => link.Occurrence));
        _mappingTargets = new(() => _links.Links.Where(link => link.Reference.Role == "mapping-target").ToLookup(link => link.Occurrence));
        foreach (var owner in index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax or EventSyntax or ReadModelSyntax))
        {
            foreach (var property in WorkspacePropertyLinks.Properties(owner.Syntax)) _owners.TryAdd(property, owner);
        }
    }

    internal bool Truncated { get; private set; }

    internal IReadOnlyList<McpLineageHop> Run(McpLineageSubject subject, McpLineageDirection direction)
    {
        var root = Place(Root(subject), null, 0, "subject");
        if (subject.Member is { } member) _reached[member] = root.Id;
        if (direction.HasFlag(McpLineageDirection.Downstream)) Expand(root, Sorted(Downstream(subject)), "downstream");
        if (direction.HasFlag(McpLineageDirection.Upstream)) Expand(root, Sorted(Upstream(subject)), "upstream");
        foreach (var hop in _hops)
        {
            var counted = hop.Direction == "subject" ? "upstream" : hop.Direction;
            hop.Branches = _hops.Count(child => child.Parent == hop.Id && child.Direction == counted);
        }

        return _hops;
    }

    static McpLineageStep[] Sorted(IEnumerable<McpLineageStep> steps) => [.. steps
        .OrderBy(step => step.Address ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(step => step.Location.Path ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(step => step.Location.Line)
        .ThenBy(step => step.Location.Column)];

    McpDeclaration? OwnerOf(McpDeclaration property) => _owners.GetValueOrDefault(property.Syntax);

    McpDeclaration? Declaration(McpReadOwner owner) => _index.Find(owner.Address, owner.Kind).FirstOrDefault();

    McpLineageStep Member(McpDeclaration property)
    {
        var role = OwnerOf(property)?.Syntax switch
        {
            EventSyntax => "event-property",
            CommandSyntax => "command-property",
            _ => "read-model-property"
        };
        var text = property.Syntax is PropertySyntax syntax ? $"{syntax.Name} {syntax.Type.Name}" : property.Name;

        return new(role, "resolved", property.Kind, property.Address, property.Location) { Text = text, Member = property };
    }

    IEnumerable<McpLineageStep> Sources(McpDeclaration property) => OwnerOf(property) switch
    {
        { Syntax: ReadModelSyntax } model => ReadModelSources(property, model),
        { Syntax: EventSyntax } @event => EventSources(property, @event),
        { Syntax: CommandSyntax } => CommandSources(property),
        _ => []
    };

    // Every hop is placed behind the budget check, so only the final truncated hop can take the count to HopBudget.
    void Expand(McpLineageHop parent, McpLineageStep[] children, string direction)
    {
        var depth = parent.Depth + 1;
        foreach (var child in children)
        {
            if (_stopped) return;
            if (_hops.Count >= HopBudget - 1)
            {
                Truncate(child, parent, depth, direction, $"The budget of {HopBudget} hops is spent; the rest of the trace is not reported.");
                _stopped = true;
                return;
            }
            if (depth > DepthBudget)
            {
                Truncate(child, parent, depth, direction, $"The depth budget of {DepthBudget} is spent; {children.Length} hop(s) below this point are not traced.");
                return;
            }
            Visit(child, parent, depth, direction);
        }
    }

    void Visit(McpLineageStep step, McpLineageHop parent, int depth, string direction)
    {
        if (step.Member is { } member && _reached.TryGetValue(member, out var first))
        {
            Place(step with { Status = "repeated", Member = null }, parent.Id, depth, direction).RepeatOf = first;
            return;
        }
        var hop = Place(step, parent.Id, depth, direction);
        if (step.Status != "resolved") return;
        if (step.Member is { } reached) _reached[reached] = hop.Id;
        Expand(hop, Sorted(Next(step, direction)), direction);
    }

    IEnumerable<McpLineageStep> Next(McpLineageStep step, string direction)
    {
        if (step.Member is not { } member) return step.Next?.Invoke() ?? [];

        return direction == "downstream" ? Downstream(member) : Sources(member);
    }

    void Truncate(McpLineageStep step, McpLineageHop parent, int depth, string direction, string reason)
    {
        Place(step with { Status = "truncated", Reason = reason, Candidates = [], Member = null, Next = null }, parent.Id, depth, direction);
        Truncated = true;
    }

    McpLineageHop Place(McpLineageStep step, int? parent, int depth, string direction)
    {
        var hop = new McpLineageHop(_hops.Count, parent, depth, direction, step with { Next = null });
        _hops.Add(hop);

        return hop;
    }
}
