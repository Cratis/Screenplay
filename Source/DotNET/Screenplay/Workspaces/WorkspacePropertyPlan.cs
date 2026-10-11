// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (entry.Node is not PropertySyntax property || entry.Address is null || entry.Parent is not { } parent || index.Find(parent) is not { } owner || owner.Node is not (CommandSyntax or EventSyntax or ReadModelSyntax)) return null;
        var scope = owner.Address!.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice or SemanticAddressPartKind.Declaration)
            .Select(part => part.Key).ToList();
        if (owner.Node is EventSyntax @event && index.Entries.Any(other => owner.Address!.Equals(other.Address) && other.Node is EventSyntax later && later.Generation > @event.Generation)) scope.Add($"generation{@event.Generation}");
        var matches = Links.Find(string.Join('.', scope.Append(property.Name)));
        if (matches is not [var declaration]) return null;
        if (!_members.TryGetValue(declaration, out var member))
        {
            member = new(entry, owner, declaration);
            _members.Add(declaration, member);
        }
        return member;
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

    internal WorkspaceSyntaxEntry MappingOwner(WorkspaceNodeHandle occurrence)
    {
        var entry = index.Find(occurrence)!;
        return entry.Parent is { } parent && index.Find(parent) is { Node: FromSyntax } from ? from : entry;
    }

    internal WorkspacePropertyCandidate Candidate(AuthoredDeclaration declaration, WorkspacePropertyLink link)
    {
        var member = Member(declaration)!;
        return new(member.Entry.Address!, member.Kind, link.Reference.Role, link.Reference.Location);
    }

    internal sealed record PropertyMember(WorkspaceSyntaxEntry Entry, WorkspaceSyntaxEntry Owner, AuthoredDeclaration Declaration)
    {
        internal string Kind => Owner.Node switch { CommandSyntax => "Command", EventSyntax => "Event", _ => "ReadModel" };
        internal WorkspacePropertyTarget Target => new(Entry.Address!, Kind, (Owner.Node as EventSyntax)?.Generation);
    }
}
