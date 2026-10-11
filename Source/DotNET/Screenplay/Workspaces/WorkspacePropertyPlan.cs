// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed class WorkspacePropertyPlan(WorkspaceSyntaxIndex index)
{
    readonly Dictionary<AuthoredDeclaration, PropertyMember> _members = [];

    internal WorkspacePropertyLinks Links { get; } = new(index);
    internal IEnumerable<PropertyMember> Members => index.Entries.Where(entry => entry.Node is PropertySyntax).Select(Member).OfType<PropertyMember>();

    internal static IEnumerable<WorkspaceSyntaxEntry> Parents(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        while (entry.Parent is { } parent && index.Find(parent) is { } ancestor)
        {
            yield return ancestor;
            entry = ancestor;
        }
    }

    internal PropertyMember? Member(WorkspaceSyntaxEntry entry)
    {
        if (entry.Node is not PropertySyntax property || entry.Address is null) return null;
        var owner = PropertyOwner(entry);
        if (owner is null) return null;
        var declaration = FindProperty(owner, property);
        return declaration is null ? null : RetainMember(entry, owner, declaration);
    }

    internal PropertyMember? Member(AuthoredDeclaration declaration) => _members.GetValueOrDefault(declaration) ?? Members.FirstOrDefault(member => member.Declaration == declaration);

    internal HashSet<AuthoredDeclaration> Chain(AuthoredDeclaration target)
    {
        var chain = new HashSet<AuthoredDeclaration> { target };
        var pending = new Queue<AuthoredDeclaration>();
        pending.Enqueue(target);
        while (pending.TryDequeue(out var current))
        {
            foreach (var link in Links.Incoming(current).Concat(Links.Outgoing(current)).Where(link => link.Resolution == "resolved" && link.OtherEnd is not null))
            {
                foreach (var other in link.Candidates.Append(link.OtherEnd!).Where(chain.Add)) pending.Enqueue(other);
            }
        }
        return chain;
    }

    internal bool Historical(WorkspaceSyntaxEntry owner)
    {
        if (owner.Node is not EventSyntax @event) return false;
        return index.Entries.Any(other => owner.Address!.Equals(other.Address) && other.Node is EventSyntax later && later.Generation > @event.Generation);
    }

    internal WorkspaceSyntaxEntry MappingOwner(WorkspaceNodeHandle occurrence)
    {
        var entry = index.Find(occurrence)!;
        return entry.Parent is { } parent && index.Find(parent) is { Node: FromSyntax } from ? from : entry;
    }

    internal SourceLocation LocationAtPosition(WorkspaceNodeHandle occurrence) => index.Entries.Single(entry => entry.Handle.Document == occurrence.Document && entry.Handle.Path == occurrence.Path).Location;

    internal WorkspacePropertyCandidate Candidate(AuthoredDeclaration declaration, WorkspacePropertyLink link)
    {
        var member = Member(declaration)!;
        return new(member.Entry.Address!, member.Kind, link.Reference.Role, link.Reference.Location);
    }

    WorkspaceSyntaxEntry? PropertyOwner(WorkspaceSyntaxEntry entry)
    {
        if (entry.Parent is not { } parent) return null;
        var owner = index.Find(parent);
        return owner?.Node is CommandSyntax or EventSyntax or ReadModelSyntax ? owner : null;
    }

    AuthoredDeclaration? FindProperty(WorkspaceSyntaxEntry owner, PropertySyntax property)
    {
        var scope = owner.Address!.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice or SemanticAddressPartKind.Declaration)
            .Select(part => part.Key).ToList();
        if (Historical(owner)) scope.Add($"generation{((EventSyntax)owner.Node).Generation}");
        var matches = Links.Find(string.Join('.', scope.Append(property.Name)));
        return matches is [var declaration] ? declaration : null;
    }

    PropertyMember RetainMember(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry owner, AuthoredDeclaration declaration)
    {
        if (_members.TryGetValue(declaration, out var retained)) return retained;
        var member = new PropertyMember(entry, owner, declaration);
        _members.Add(declaration, member);
        return member;
    }

    internal sealed record PropertyMember(WorkspaceSyntaxEntry Entry, WorkspaceSyntaxEntry Owner, AuthoredDeclaration Declaration)
    {
        internal string Kind => Owner.Node switch { CommandSyntax => "Command", EventSyntax => "Event", _ => "ReadModel" };
        internal WorkspacePropertyTarget Target => new(Entry.Address!, Kind, (Owner.Node as EventSyntax)?.Generation);
        internal WorkspacePropertyCandidate DeclarationCandidate => new(Entry.Address!, Kind, "declaration", Entry.Location);
    }
}
