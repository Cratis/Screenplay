// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Workspaces;

// Declaration connectivity is needed before the new property exists. Never connect by property name.
sealed class WorkspacePropertyAddChain(WorkspaceSyntaxIndex index, WorkspacePropertyPlan plan)
{
    internal IEnumerable<WorkspaceSyntaxEntry> Owners => index.Entries.Where(entry => entry.Node is CommandSyntax or EventSyntax or ReadModelSyntax);
    internal IEnumerable<WorkspacePropertyFeed> Feeds => plan.Links.AdditionFeeds;
    internal IEnumerable<WorkspaceSyntaxEntry> Producers => index.Entries.Where(entry => entry.Node is ProducesSyntax or CaptureAppendSyntax);

    internal WorkspaceSyntaxEntry FeedBlock(WorkspacePropertyFeed feed) => plan.MappingOwner(feed.Entry.Handle);

    internal WorkspaceSyntaxEntry? Entry(AuthoredDeclaration declaration) => Owners.FirstOrDefault(entry => plan.Links.AdditionOwner(entry) == declaration);
    internal WorkspaceSyntaxEntry[] Targets(WorkspaceSyntaxEntry block) => [.. plan.Links.AdditionOwners(block).Select(Entry).OfType<WorkspaceSyntaxEntry>()];

    internal HashSet<WorkspaceNodeHandle> Connected(WorkspaceSyntaxEntry owner)
    {
        var edges = Edges().ToArray();
        var connected = new HashSet<WorkspaceNodeHandle> { owner.Handle };
        var pending = new Queue<WorkspaceNodeHandle>();
        pending.Enqueue(owner.Handle);
        while (pending.TryDequeue(out var current))
        {
            foreach (var edge in edges.Where(edge => edge.Source == current || edge.Target == current))
            {
                var next = edge.Source == current ? edge.Target : edge.Source;
                if (connected.Add(next)) pending.Enqueue(next);
            }
        }
        return connected;
    }

    internal bool BlockOnChain(WorkspaceSyntaxEntry block, HashSet<WorkspaceNodeHandle> connected) =>
        Targets(block).Any(target => connected.Contains(target.Handle)) || Feeds.Any(feed => FeedBlock(feed).Handle == block.Handle && feed.Models.Concat(feed.Events).Select(Entry).OfType<WorkspaceSyntaxEntry>().Any(entry => connected.Contains(entry.Handle)));

    static bool DirectFeed(WorkspacePropertyFeed feed) => (feed.Nested, feed.Events.Length, feed.Models.Length) is (null, 1, 1);

    IEnumerable<(WorkspaceNodeHandle Source, WorkspaceNodeHandle Target)> Edges() => PropertyEdges().Concat(ProductionEdges()).Concat(ProjectionEdges());

    IEnumerable<(WorkspaceNodeHandle Source, WorkspaceNodeHandle Target)> PropertyEdges()
    {
        foreach (var link in plan.Links.Links.Where(link => link.Resolution == "resolved" && link.OtherEnd is not null))
        {
            var source = plan.Member(link.OtherEnd!);
            foreach (var target in link.Candidates.Select(plan.Member).OfType<WorkspacePropertyPlan.PropertyMember>())
            {
                if (source is not null) yield return (source.Owner.Handle, target.Owner.Handle);
            }
        }
    }

    IEnumerable<(WorkspaceNodeHandle Source, WorkspaceNodeHandle Target)> ProductionEdges()
    {
        foreach (var producer in Producers)
        {
            var source = WorkspacePropertyPlan.Parents(producer, index).FirstOrDefault(entry => entry.Node is CommandSyntax);
            var targets = Targets(producer);
            if ((source, targets) is ({ } owner, [var target])) yield return (owner.Handle, target.Handle);
        }
    }

    IEnumerable<(WorkspaceNodeHandle Source, WorkspaceNodeHandle Target)> ProjectionEdges()
    {
        foreach (var feed in Feeds.Where(DirectFeed))
        {
            if ((Entry(feed.Events[0]), Entry(feed.Models[0])) is ({ } source, { } target)) yield return (source.Handle, target.Handle);
        }
    }
}

sealed record WorkspacePropertyAddition(WorkspaceSyntaxEntry Owner, string Name, PropertySyntax Property, string AuthoredAddress)
{
    internal string Kind => Owner.Node switch { CommandSyntax => "Command", EventSyntax => "Event", _ => "ReadModel" };
}
