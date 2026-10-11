// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyAutoMapContinuity(WorkspacePropertyPlan before, WorkspacePropertyRequest request, IReadOnlyList<WorkspacePropertyPlan.PropertyMember> selected)
{
    internal WorkspacePropertyRefusal? Inspect(ScreenplayWorkspace candidate)
    {
        if (request.Action != "rename") return null;
        var expected = Edges(before).Select(PlannedKey).ToHashSet();
        var after = new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(candidate));
        var introduced = Edges(after).Where(edge => !expected.Contains(ActualKey(edge))).ToArray();
        return introduced.Length == 0 ? null : new(WorkspaceConflictKind.AmbiguousPropertyReference, [.. introduced.Select(OriginalCandidate).Distinct()]);
    }

    static IEnumerable<AutoMapEdge> Edges(WorkspacePropertyPlan plan)
    {
        foreach (var link in plan.Links.Links.Where(link => link.Reference.Role == "automap" && link.Resolution == "resolved"))
        {
            if (Edge(plan, link) is { } edge) yield return edge;
        }
    }

    static AutoMapEdge? Edge(WorkspacePropertyPlan plan, WorkspacePropertyLink link)
    {
        if (link.Candidates is not [var declaration]) return null;
        if (link.OtherEnd is null) return null;
        var target = plan.Member(declaration);
        if (target?.Kind != "ReadModel") return null;
        var source = plan.Member(link.OtherEnd);
        return source is null ? null : new(link, source, target);
    }

    static AutoMapKey Key(AutoMapEdge edge, Func<WorkspacePropertyPlan.PropertyMember, string> name) => new(
        new(edge.Link.Occurrence.Document, edge.Link.Occurrence.Path),
        new(edge.Source.Owner.Address!, name(edge.Source)),
        new(edge.Target.Owner.Address!, name(edge.Target)));

    static AutoMapKey ActualKey(AutoMapEdge edge) => Key(edge, member => member.Declaration.Name);

    AutoMapKey PlannedKey(AutoMapEdge edge) => Key(edge, PlannedName);

    string PlannedName(WorkspacePropertyPlan.PropertyMember member) => selected.Contains(member) ? request.NewName! : member.Declaration.Name;

    WorkspacePropertyCandidate OriginalCandidate(AutoMapEdge edge)
    {
        var source = before.Members.Single(member => CurrentSourceMatches(member, edge));
        return new(source.Entry.Address!, source.Kind, "automap", before.LocationAtPosition(edge.Link.Occurrence));
    }

    bool CurrentSourceMatches(WorkspacePropertyPlan.PropertyMember member, AutoMapEdge edge) =>
        !before.Historical(member.Owner) && member.Owner.Address!.Equals(edge.Source.Owner.Address) && PlannedName(member) == edge.Source.Declaration.Name;

    sealed record AutoMapEdge(WorkspacePropertyLink Link, WorkspacePropertyPlan.PropertyMember Source, WorkspacePropertyPlan.PropertyMember Target);
    sealed record AutoMapPosition(DocumentId Document, string Path);
    sealed record PropertyEndpoint(SemanticAddress Owner, string Name);
    sealed record AutoMapKey(AutoMapPosition Position, PropertyEndpoint Source, PropertyEndpoint Target);
}
