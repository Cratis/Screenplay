// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class WorkspacePropertySafety
{
    internal static WorkspacePropertyRefusal? Inspect(WorkspacePropertyPlan plan, WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyRequest request, HashSet<AuthoredDeclaration> selected)
    {
        var links = plan.Links.Incoming(member.Declaration).ToArray();
        var opaque = links.Where(link => link.Reference.Role.Equals("opaque", StringComparison.Ordinal) || link.Reference.Role.Equals("unclassified", StringComparison.Ordinal)).ToArray();
        if (opaque.Length > 0) return Refuse(WorkspaceConflictKind.OpaquePropertyReference, plan, member, opaque);
        var nested = links.Where(link => link.Reference.Name.Contains('.') && link.Resolution != "resolved").ToArray();
        if (nested.Length > 0) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, plan, member, nested);
        var incomplete = links.Where(link => link.Resolution != "resolved").ToArray();
        if (incomplete.Length > 0) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, plan, member, incomplete);
        if (ReadModelKey(member, links)) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, plan, member, links.Where(link => link.Reference.Role == "key"));
        if (request.Action == "remove")
        {
            var readers = links.Where(link => ReaderRemains(link, selected)).ToArray();
            if (readers.Length > 0) return Refuse(WorkspaceConflictKind.PropertyReadersExist, plan, member, readers);
        }
        else
        {
            if (WorkspacePropertyLinks.Properties(member.Owner.Node).Any(property => property.Name == request.NewName && property.Name != member.Declaration.Name))
            {
                return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, plan, member, []);
            }
            var screens = links.Where(link => link.Reference.Role == "screen-binding").ToArray();
            if (screens.Length > 0) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, plan, member, screens);
            var autoMapCaptures = links.Where(link => link.Reference.Role == "automap" && link.OtherEnd is not null && plan.Member(link.OtherEnd) is { } other &&
                WorkspacePropertyLinks.Properties(other.Owner.Node).Any(property => property.Name.Equals(request.NewName, StringComparison.OrdinalIgnoreCase) && property.Name != other.Declaration.Name)).ToArray();
            if (autoMapCaptures.Length > 0) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, plan, member, autoMapCaptures);
            var sharedMappings = links.Where(link => link.Reference.Role == "automap" && SharedSourcesDiffer(plan, member, link, request, selected)).ToArray();
            if (sharedMappings.Length > 0) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, plan, member, sharedMappings);
        }
        return null;
    }

    static bool SharedSourcesDiffer(WorkspacePropertyPlan plan, WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyLink link, WorkspacePropertyRequest request, HashSet<AuthoredDeclaration> selected)
    {
        var target = member.Kind == "ReadModel" ? member.Declaration : link.OtherEnd!;
        var owner = plan.MappingOwner(link.Occurrence).Handle;
        return plan.Links.Incoming(target).Where(source => source.Reference.Role == "automap" && source.OtherEnd is not null && plan.MappingOwner(source.Occurrence).Handle == owner)
            .Select(source => selected.Contains(source.OtherEnd!) ? request.NewName! : source.OtherEnd!.Name).Distinct(StringComparer.Ordinal).Skip(1).Any();
    }

    static bool ReadModelKey(WorkspacePropertyPlan.PropertyMember member, IEnumerable<WorkspacePropertyLink> links) => member.Owner.Node is ReadModelSyntax &&
        (((PropertySyntax)member.Entry.Node).IsKey || links.Any(link => link.Reference.Role == "key") ||
         (member.Declaration.Name.EndsWith("Id", StringComparison.Ordinal) && WorkspacePropertyLinks.Properties(member.Owner.Node).Count(property => property.Name.EndsWith("Id", StringComparison.Ordinal)) == 1));

    static bool ReaderRemains(WorkspacePropertyLink link, HashSet<AuthoredDeclaration> selected) => link.Reference.Role switch
    {
        "mapping-source" or "automap" => link.OtherEnd is null || !selected.Contains(link.OtherEnd),
        "mapping-target" or "fixture" or "assertion" => false,
        _ => true
    };

    static WorkspacePropertyRefusal Refuse(WorkspaceConflictKind kind, WorkspacePropertyPlan plan, WorkspacePropertyPlan.PropertyMember member, IEnumerable<WorkspacePropertyLink> links)
    {
        var candidates = links.Select(link => plan.Candidate(member.Declaration, link)).Distinct().ToImmutableArray();
        return new(kind, candidates.IsEmpty ? [new(member.Entry.Address!, member.Kind, "declaration", member.Entry.Location)] : candidates);
    }
}

internal sealed record WorkspacePropertyRefusal(WorkspaceConflictKind Kind, ImmutableArray<WorkspacePropertyCandidate> Candidates);
