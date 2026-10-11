// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertySafety(WorkspacePropertyPlan plan, WorkspacePropertyRequest request, HashSet<AuthoredDeclaration> selected)
{
    internal static WorkspacePropertyRefusal? Inspect(WorkspacePropertyPlan plan, WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyRequest request, HashSet<AuthoredDeclaration> selected) =>
        new WorkspacePropertySafety(plan, request, selected).InspectMember(member);

    static bool SameExpressionOccurrence(WorkspacePropertyLink left, WorkspacePropertyLink right) =>
        left.Occurrence == right.Occurrence && left.Reference.Location == right.Reference.Location && left.Reference.Name == right.Reference.Name;

    static bool ReadModelKey(MemberInspection inspection)
    {
        var member = inspection.Member;
        if (member.Owner.Node is not ReadModelSyntax) return false;
        if (((PropertySyntax)member.Entry.Node).IsKey) return true;
        if (inspection.Links.Any(link => link.Reference.Role == "key")) return true;
        if (!member.Declaration.Name.EndsWith("Id", StringComparison.Ordinal)) return false;
        return WorkspacePropertyLinks.Properties(member.Owner.Node).Count(property => property.Name.EndsWith("Id", StringComparison.Ordinal)) == 1;
    }

    WorkspacePropertyRefusal? InspectMember(WorkspacePropertyPlan.PropertyMember member)
    {
        var inspection = new MemberInspection(member, [.. plan.Links.Incoming(member.Declaration)]);
        if (Coverage(inspection) is { } refusal) return refusal;
        if (ReadModelKey(inspection)) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, inspection, inspection.Links.Where(link => link.Reference.Role == "key"));
        return request.Action == "remove" ? Removal(inspection) : Rename(inspection);
    }

    WorkspacePropertyRefusal? Coverage(MemberInspection inspection)
    {
        var opaque = inspection.Links.Where(link => link.Reference.Role.Equals("opaque", StringComparison.Ordinal) || link.Reference.Role.Equals("unclassified", StringComparison.Ordinal)).ToArray();
        if (opaque.Length > 0) return Refuse(WorkspaceConflictKind.OpaquePropertyReference, inspection, opaque);
        var nested = inspection.Links.Where(link => link.Reference.Name.Contains('.') && link.Resolution != "resolved").ToArray();
        if (nested.Length > 0) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, inspection, nested);
        var incomplete = inspection.Links.Where(link => link.Resolution != "resolved").ToArray();
        return incomplete.Length > 0 ? Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, inspection, incomplete) : null;
    }

    WorkspacePropertyRefusal? Removal(MemberInspection inspection)
    {
        var readers = inspection.Links.Where(ReaderRemains).ToArray();
        return readers.Length > 0 ? Refuse(WorkspaceConflictKind.PropertyReadersExist, inspection, readers) : null;
    }

    WorkspacePropertyRefusal? Rename(MemberInspection inspection)
    {
        if (OwnerCollision(inspection.Member)) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, inspection, []);
        var screens = inspection.Links.Where(link => link.Reference.Role == "screen-binding").ToArray();
        if (screens.Length > 0) return Refuse(WorkspaceConflictKind.UnsupportedPropertySpan, inspection, screens);
        var captures = inspection.Links.Where(AutoMapCaptures).ToArray();
        if (captures.Length > 0) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, inspection, captures);
        var shared = inspection.Links.Where(link => link.Reference.Role == "automap" && SharedSourcesDiffer(inspection.Member, link)).ToArray();
        if (shared.Length > 0) return Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, inspection, shared);
        var explicitMappings = inspection.Links.Where(ExplicitSourcesDiffer).ToArray();
        return explicitMappings.Length > 0 ? Refuse(WorkspaceConflictKind.AmbiguousPropertyReference, inspection, explicitMappings) : null;
    }

    bool OwnerCollision(WorkspacePropertyPlan.PropertyMember member) => WorkspacePropertyLinks.Properties(member.Owner.Node)
        .Any(property => property.Name == request.NewName && property.Name != member.Declaration.Name);

    bool AutoMapCaptures(WorkspacePropertyLink link)
    {
        if (link.Reference.Role != "automap") return false;
        if (link.OtherEnd is null) return false;
        var other = plan.Member(link.OtherEnd);
        if (other is null) return false;
        return WorkspacePropertyLinks.Properties(other.Owner.Node).Any(property => property.Name.Equals(request.NewName, StringComparison.OrdinalIgnoreCase) && property.Name != other.Declaration.Name);
    }

    bool SharedSourcesDiffer(WorkspacePropertyPlan.PropertyMember member, WorkspacePropertyLink link)
    {
        var target = member.Kind == "ReadModel" ? member.Declaration : link.OtherEnd!;
        var owner = plan.MappingOwner(link.Occurrence).Handle;
        return plan.Links.Incoming(target).Where(source => source.Reference.Role == "automap" && source.OtherEnd is not null && plan.MappingOwner(source.Occurrence).Handle == owner)
            .Select(source => selected.Contains(source.OtherEnd!) ? request.NewName! : source.OtherEnd!.Name).Distinct(StringComparer.Ordinal).Skip(1).Any();
    }

    bool ExplicitSourcesDiffer(WorkspacePropertyLink link)
    {
        if (link.Reference.Role != "mapping-source") return false;
        var consumers = plan.Links.Links.Where(other => other.Reference.Role == "mapping-source" && SameExpressionOccurrence(other, link));
        return consumers.SelectMany(other => other.Candidates).Select(PlannedName).Distinct(StringComparer.Ordinal).Skip(1).Any();
    }

    string PlannedName(AuthoredDeclaration declaration) => selected.Contains(declaration) ? request.NewName! : declaration.Name;

    bool ReaderRemains(WorkspacePropertyLink link) => link.Reference.Role switch
    {
        "mapping-source" or "automap" => link.OtherEnd is null || !selected.Contains(link.OtherEnd),
        "mapping-target" or "fixture" or "assertion" => false,
        _ => true
    };

    WorkspacePropertyRefusal Refuse(WorkspaceConflictKind kind, MemberInspection inspection, IEnumerable<WorkspacePropertyLink> links)
    {
        var candidates = links.Select(link => plan.Candidate(inspection.Member.Declaration, link)).Distinct().ToImmutableArray();
        return new(kind, candidates.IsEmpty ? [inspection.Member.DeclarationCandidate] : candidates);
    }

    sealed record MemberInspection(WorkspacePropertyPlan.PropertyMember Member, WorkspacePropertyLink[] Links);
}

internal sealed record WorkspacePropertyRefusal(WorkspaceConflictKind Kind, ImmutableArray<WorkspacePropertyCandidate> Candidates);
