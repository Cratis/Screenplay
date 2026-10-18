// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

sealed record WorkspacePropertyLink(AuthoredReference Reference, AuthoredDeclaration[] Candidates, AuthoredDeclaration? OtherEnd, WorkspaceNodeHandle Occurrence)
{
    internal string Resolution => new ReferenceEdge(Reference, Candidates).Resolution;
}

// Property targets deliberately live outside AuthoringIndex.Declarations. A member is not a declaration
// in model navigation, and these links must not change declaration rename's reference domain.
internal sealed partial class WorkspacePropertyLinks
{
    readonly AuthoringIndex _index;
    readonly WorkspaceSyntaxEntry[] _entries;
    readonly Dictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _handles;
    readonly Dictionary<SyntaxNode, AuthoredDeclaration> _owners;
    readonly Dictionary<AuthoredDeclaration, AuthoredDeclaration[]> _properties = new(ReferenceEqualityComparer.Instance);
    readonly List<WorkspacePropertyLink> _links = [];
    readonly List<WorkspacePropertyProjectionEffect> _projectionEffects = [];
    readonly List<WorkspacePropertyFeed> _additionFeeds = [];

    internal WorkspacePropertyLinks(WorkspaceSyntaxIndex index)
        : this(index.Entries, new AuthoringSnapshot(index.Workspace.Documents).Index)
    {
    }

    internal WorkspacePropertyLinks(IEnumerable<WorkspaceSyntaxEntry> entries, AuthoringIndex index)
    {
        _index = index;
        _entries = [.. entries];
        _handles = _entries.ToDictionary(entry => entry.Handle);
        _owners = new(ReferenceEqualityComparer.Instance);
        IndexOwners();
        IndexProperties();
        IndexMappings();
        IndexCaseValues();
        IndexProjections();
        IndexReaders();
        IndexInteractions();
        IndexCompleteness();
    }

    internal IEnumerable<WorkspacePropertyFeed> AdditionFeeds => _additionFeeds;

    internal IEnumerable<WorkspacePropertyLink> Links => _links;
    internal IEnumerable<WorkspacePropertyProjectionEffect> ProjectionEffects => _projectionEffects;

    internal static IEnumerable<PropertySyntax> Properties(SyntaxNode node) => node switch
    {
        CommandSyntax command => command.Properties,
        EventSyntax @event => @event.Properties,
        ReadModelSyntax model => model.Properties,
        _ => []
    };

    internal static IEnumerable<WorkspaceSyntaxEntry> Entries(IEnumerable<ApplicationSyntax> applications)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("property-index"));
        var ordinal = 0;
        foreach (var application in applications)
        {
            var prefix = $"/applications/{ordinal++}";
            foreach (var entry in WorkspaceSyntaxIndex.ForSyntax(application, catalog))
            {
                yield return entry with
                {
                    Handle = entry.Handle with { Path = prefix + entry.Handle.Path },
                    Parent = entry.Parent is { } parent ? parent with { Path = prefix + parent.Path } : null
                };
            }
        }
    }

    internal AuthoredDeclaration[] AdditionOwners(WorkspaceSyntaxEntry entry) => FixtureOwner(entry);
    internal string AdditionFixtureRole(WorkspaceSyntaxEntry entry) => FixtureRole(entry);
    internal AuthoredDeclaration? AdditionOwner(WorkspaceSyntaxEntry entry) => Owner(entry);

    internal SyntaxNode? NodeAt(WorkspaceNodeHandle handle) => _handles.TryGetValue(handle, out var entry) ? entry.Node : null;

    internal SyntaxNode? ProductionOf(WorkspaceNodeHandle handle) => _handles.TryGetValue(handle, out var entry) ? Parents(entry).FirstOrDefault(parent => ProductionStep(parent.Node))?.Node : null;

    internal AuthoredDeclaration? OwnerOf(WorkspaceSyntaxEntry entry) => Owner(entry);

    internal AuthoredDeclaration[] Find(string address) => [.. _properties.Values.SelectMany(properties => properties).Where(property => property.Address == address)];

    internal IEnumerable<WorkspacePropertyLink> Incoming(AuthoredDeclaration property) => _links.Where(link => link.Candidates.Contains(property));

    internal IEnumerable<WorkspacePropertyLink> Outgoing(AuthoredDeclaration property) => _links.Where(link => link.OtherEnd == property)
        .Concat(Incoming(property).Where(link => link.OtherEnd is not null && !HasCounterpart(property, link))
            .Select(link => link with { Candidates = [link.OtherEnd!], OtherEnd = property }));

    internal bool Complete(AuthoredDeclaration property) => Incoming(property).All(link => link.Resolution == "resolved");

    static bool Connects(WorkspacePropertyLink link, AuthoredDeclaration property, AuthoredDeclaration? other) => link.OtherEnd == property && link.Candidates.Contains(other);

    static AuthoredDeclaration[] Current(IEnumerable<AuthoredDeclaration> declarations) => [.. declarations.Where(declaration => !Historical(declaration, declarations))];

    static bool Historical(AuthoredDeclaration declaration, IEnumerable<AuthoredDeclaration> declarations) => declaration.Syntax is EventSyntax @event &&
        declarations.Any(other => other.Kind == "Event" && other.Address == declaration.Address && ((EventSyntax)other.Syntax).Generation > @event.Generation);

    bool HasCounterpart(AuthoredDeclaration property, WorkspacePropertyLink occurrence) => _links.Exists(link =>
        link.Occurrence == occurrence.Occurrence && Connects(link, property, occurrence.OtherEnd));

    WorkspaceNodeHandle MappingOccurrence(WorkspaceSyntaxEntry use) => new[] { use }.Concat(Parents(use))
        .FirstOrDefault(entry => entry.Node is PropertyMappingSyntax or MappingSyntax)?.Handle ?? use.Handle;

    void IndexOwners()
    {
        var declarations = _index.Declarations.SelectMany(declaration => declaration.Parts.Select(part => (part, declaration)))
            .ToLookup(pair => (pair.part.GetType(), pair.part.Location, Name: WorkspaceReferenceBindings.Name(pair.part)));
        foreach (var entry in _entries)
        {
            var matches = declarations[(entry.Node.GetType(), entry.Location, WorkspaceReferenceBindings.Name(entry.Node))].Select(pair => pair.declaration).Distinct().Take(2).ToArray();
            if (matches is [var owner]) _owners[entry.Node] = owner;
        }
    }

    void IndexProperties()
    {
        foreach (var owner in _index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax or EventSyntax or ReadModelSyntax))
        {
            IndexProperties(owner);
        }
    }

    void IndexProperties(AuthoredDeclaration owner)
    {
        var historical = Historical(owner, _index.Declarations);
        var scope = owner.Address.Split('.');
        if (historical) scope = [.. scope, $"generation{((EventSyntax)owner.Syntax).Generation}"];
        _properties[owner] = [.. Properties(owner.Syntax).Select(property => new AuthoredDeclaration("Property", property.Name, scope, property.Location, null, new { owner = owner.Owner, generation = (owner.Syntax as EventSyntax)?.Generation, historical }, property))];
    }

    IEnumerable<WorkspaceSyntaxEntry> Parents(WorkspaceSyntaxEntry entry)
    {
        while (entry.Parent is { } parent && _handles.TryGetValue(parent, out var ancestor))
        {
            yield return ancestor;
            entry = ancestor;
        }
    }

    AuthoredDeclaration? Owner(WorkspaceSyntaxEntry entry) => new[] { entry }.Concat(Parents(entry)).Select(DeclarationOwner).FirstOrDefault(owner => owner is not null);

    AuthoredDeclaration? DeclarationOwner(WorkspaceSyntaxEntry entry) => _owners.GetValueOrDefault(entry.Node) ??
        (_handles.TryGetValue(entry.Handle, out var original) ? _owners.GetValueOrDefault(original.Node) : null);

    AuthoredDeclaration[] Resolve(string name, string[] kinds, WorkspaceSyntaxEntry use)
    {
        var owner = Owner(use);
        var scope = owner?.Scope ?? [];
        return Current(_index.Resolve(new(name, kinds, scope, use.Location, "property-owner", owner?.Owner)));
    }

    AuthoredDeclaration[] ResolveAtOwner(string name, string[] kinds, AuthoredDeclaration owner, SourceLocation location) => Current(_index.Resolve(new(name, kinds, owner.Scope, location, "property-owner", owner.Owner)));

    AuthoredDeclaration[] Members(IEnumerable<AuthoredDeclaration> owners, string path) => [.. owners.SelectMany(owner => _properties.GetValueOrDefault(owner) ?? [])
        .Where(property => property.Name == path.Split('.')[0])];

    ReadOwner? ReferenceOwner(WorkspaceSyntaxEntry use)
    {
        var seed = new[] { use }.Concat(Parents(use)).Select(entry => entry.Node).OfType<SeedEventSyntax>().FirstOrDefault();

        return seed is not null ? new("SeedEvent", seed.Event, seed.Event, seed.Location) : Owner(use)?.Owner;
    }

    void Add(WorkspaceSyntaxEntry use, string role, PropertyReference property)
    {
        var owner = Owner(use);
        var reference = new AuthoredReference(property.Text, ["Property"], owner?.Scope ?? [], use.Location, role, ReferenceOwner(use))
        {
            AmbiguousSourceOwner = property.Candidates.Length > 1,
            IncompleteSourceOwner = property.Incomplete || role == "opaque",
            SourceConfidenceReasons = role == "unclassified" ? ["Property name occurs in referencing declaration text without a classified link."] : []
        };
        _links.Add(new(reference, property.Candidates, property.Other, MappingOccurrence(use)));
    }

    sealed record PropertyReference(string Text, AuthoredDeclaration[] Candidates, AuthoredDeclaration? Other = null, bool Incomplete = false);
}
