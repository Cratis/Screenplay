// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed record WorkspacePropertyLink(AuthoredReference Reference, AuthoredDeclaration[] Candidates, AuthoredDeclaration? OtherEnd)
{
    internal string Resolution => new ReferenceEdge(Reference, Candidates).Resolution;
}

// Property targets deliberately live outside AuthoringIndex.Declarations. A member is not a declaration
// in model navigation, and these links must not change declaration rename's reference domain.
internal sealed partial class WorkspacePropertyLinks
{
    static readonly string[] _assertionMembers = ["thenEvents", "thenReadModels", "thenQueries", "thenResults", "results"];
    readonly AuthoringIndex _index;
    readonly WorkspaceSyntaxEntry[] _entries;
    readonly Dictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _handles;
    readonly Dictionary<SyntaxNode, AuthoredDeclaration> _owners;
    readonly Dictionary<AuthoredDeclaration, AuthoredDeclaration[]> _properties = new(ReferenceEqualityComparer.Instance);
    readonly List<WorkspacePropertyLink> _links = [];

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
        var declarations = index.Declarations.SelectMany(declaration => declaration.Parts.Select(part => (part, declaration)))
            .ToLookup(pair => (pair.part.GetType(), pair.part.Location, Name: WorkspaceReferenceBindings.Name(pair.part)));
        foreach (var entry in _entries)
        {
            var matches = declarations[(entry.Node.GetType(), entry.Location, WorkspaceReferenceBindings.Name(entry.Node))].Select(pair => pair.declaration).Distinct().Take(2).ToArray();
            if (matches is [var owner]) _owners[entry.Node] = owner;
        }
        foreach (var owner in index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax or EventSyntax or ReadModelSyntax))
        {
            var historical = owner.Syntax is EventSyntax @event && index.Declarations.Any(other => other.Kind == "Event" && other.Address == owner.Address && ((EventSyntax)other.Syntax).Generation > @event.Generation);
            var scope = owner.Address.Split('.');
            if (historical) scope = [.. scope, $"generation{((EventSyntax)owner.Syntax).Generation}"];
            _properties[owner] = [.. Properties(owner.Syntax).Select(property => new AuthoredDeclaration("Property", property.Name, scope, property.Location, null, new { owner = owner.Owner, generation = (owner.Syntax as EventSyntax)?.Generation, historical }, property))];
        }
        IndexMappings();
        IndexCaseValues();
        IndexProjections();
        IndexReaders();
    }

    internal IEnumerable<WorkspacePropertyLink> Links => _links;

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

    internal AuthoredDeclaration[] Find(string address) => [.. _properties.Values.SelectMany(properties => properties).Where(property => property.Address == address)];

    internal IEnumerable<WorkspacePropertyLink> Incoming(AuthoredDeclaration property) => _links.Where(link => link.Candidates.Contains(property));

    internal IEnumerable<WorkspacePropertyLink> Outgoing(AuthoredDeclaration property) => _links.Where(link => link.OtherEnd == property);

    internal bool Complete(AuthoredDeclaration property) => Incoming(property).All(link => link.Resolution == "resolved");

    static AuthoredDeclaration[] Current(IEnumerable<AuthoredDeclaration> declarations) => [.. declarations.Where(declaration => declaration.Syntax is not EventSyntax @event ||
        !declarations.Any(other => other.Kind == "Event" && other.Address == declaration.Address && ((EventSyntax)other.Syntax).Generation > @event.Generation))];

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

    void Add(WorkspaceSyntaxEntry use, string role, AuthoredDeclaration[] candidates, string text, AuthoredDeclaration? other = null, bool incomplete = false)
    {
        var owner = Owner(use);
        var reference = new AuthoredReference(text, ["Property"], owner?.Scope ?? [], use.Location, role, owner?.Owner)
        {
            AmbiguousSourceOwner = candidates.Length > 1,
            IncompleteSourceOwner = incomplete || role == "opaque"
        };
        _links.Add(new(reference, candidates, other));
    }

    void IndexMappings()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is PropertyMappingSyntax))
        {
            var mapping = (PropertyMappingSyntax)entry.Node;
            var ancestors = Parents(entry).ToArray();
            if (ancestors.Any(ancestor => ancestor.Node is SpecificationStreamSyntax or SpecificationCaseSyntax)) continue;
            var step = ancestors.FirstOrDefault(ancestor => ancestor.Node is ProducesSyntax or CaptureAppendSyntax or InvokesSyntax or SpecificationExampleSyntax or SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQueryResultSyntax or SpecificationQuerySyntax or SpecificationWhenQuerySyntax);
            if (step is null) continue;
            var targets = FixtureOwner(step);
            var target = Members(targets, mapping.Property);
            var production = step.Node is ProducesSyntax or CaptureAppendSyntax or InvokesSyntax;
            var sources = production ? ExpressionSources(mapping.Source, entry).ToArray() : [];
            var source = sources.SelectMany(item => item.Candidates).Distinct().ToArray();
            var role = production ? "mapping-target" : FixtureRole(step);
            var ambiguousProduction = step.Node is ProducesSyntax produced && _index.ResolveProduction(produced.Event, Owner(step)?.Scope ?? []).Targets.Length != 1;
            Add(entry, role, target, mapping.Property, source is [var only] ? only : null, target.Length != 1 || ambiguousProduction || mapping.Property.Contains('.') || sources.Any(item => item.Candidates.Length != 1 || item.Incomplete) || (production && mapping.Source is RawExpressionSyntax));
            foreach (var item in sources)
            {
                Add(item.Entry, "mapping-source", item.Candidates, item.Path, !ambiguousProduction && target is [var onlyTarget] ? onlyTarget : null, target.Length != 1 || ambiguousProduction || item.Incomplete);
            }
        }
    }

    AuthoredDeclaration[] FixtureOwner(WorkspaceSyntaxEntry step)
    {
        if (step.Node is ProducesSyntax production)
        {
            return Current(_index.ResolveProduction(production.Event, Owner(step)?.Scope ?? []).Targets.Where(target => target.Kind == "Event"));
        }
        (string Name, string[] Kinds) reference = step.Node switch
        {
            CaptureAppendSyntax value => (value.Event, ["Event"]),
            InvokesSyntax value => (value.Command, ["Command"]),
            SpecificationExampleSyntax value => (value.Type, ["Command", "Event", "ReadModel"]),
            SpecificationCommandSyntax value => (value.CommandType, ["Command", "Example"]),
            SpecificationEventSyntax value => (value.EventType, ["Event", "Example"]),
            SpecificationReadModelSyntax value => (value.Name, ["ReadModel", "Example"]),
            SpecificationQueryResultSyntax => (QueryName(step), ["Query"]),
            SpecificationQuerySyntax value => (value.Query, ["Query"]),
            SpecificationWhenQuerySyntax value => (value.Query, ["Query"]),
            _ => (string.Empty, [])
        };
        var targets = Resolve(reference.Name, reference.Kinds, step);
        if (targets is [var target] && target.Syntax is SpecificationExampleSyntax example) targets = ResolveAtOwner(example.Type, ["Command", "Event", "ReadModel"], target, step.Location);
        if (targets is [var query] && query.Syntax is QuerySyntax declared) targets = ResolveAtOwner(declared.ReturnType.Name, ["ReadModel"], query, step.Location);
        return targets;
    }

    string QueryName(WorkspaceSyntaxEntry entry)
    {
        var query = Parents(entry).Select(parent => parent.Node).OfType<SpecificationQuerySyntax>().FirstOrDefault();
        var specification = Parents(entry).Select(parent => parent.Node).OfType<SpecificationSyntax>().FirstOrDefault();
        return query?.Query ?? specification?.WhenQuery?.Query ?? string.Empty;
    }

    bool IsAssertion(WorkspaceSyntaxEntry step) => new[] { step }.Concat(Parents(step)).Any(entry => _assertionMembers.Contains(entry.Member, StringComparer.Ordinal));

    string FixtureRole(WorkspaceSyntaxEntry step) => step.Node is not (SpecificationQuerySyntax or SpecificationWhenQuerySyntax) && IsAssertion(step) ? "assertion" : "fixture";

    IEnumerable<(WorkspaceSyntaxEntry Entry, string Path, AuthoredDeclaration[] Candidates, bool Incomplete)> ExpressionSources(ExpressionSyntax expression, WorkspaceSyntaxEntry use)
    {
        foreach (var entry in _entries.Where(entry => ReferenceEquals(entry.Node, expression) || Parents(entry).Any(parent => ReferenceEquals(parent.Node, expression))))
        {
            var path = entry.Node switch
            {
                PathExpressionSyntax value => value.Path,
                ContextExpressionSyntax value when value.Path.StartsWith("command.", StringComparison.Ordinal) => value.Path[8..],
                SourceItemExpressionSyntax value => $"$.{value.Path}",
                _ => null
            };
            if (path is null) continue;
            yield return (entry, path, SourceMembers(use, path), NestedSource(use, path));
        }
    }

    bool NestedSource(WorkspaceSyntaxEntry use, string path)
    {
        if (path.StartsWith("$context.command.", StringComparison.Ordinal)) path = path[17..];
        var parts = path.Split('.');
        if (parts.Length == 1) return false;

        return parts.Length > 2 || SourceMembers(use, parts[0]).Length > 0 || SourceMembers(use, path).Length != 1;
    }

    AuthoredDeclaration[] SourceMembers(WorkspaceSyntaxEntry use, string path)
    {
        if (path.StartsWith("$context.command.", StringComparison.Ordinal)) path = path[17..];
        var command = new[] { use }.Concat(Parents(use)).FirstOrDefault(entry => entry.Node is CommandSyntax);
        if (command is not null && Owner(command) is { } declared)
        {
            var local = Members([declared], path);
            if (local.Length > 0 || !path.Contains('.')) return local;
            var reads = ((CommandSyntax)command.Node).Reads ?? [];
            var parts = path.Split('.', 2);
            var read = reads.Where(read => read.ReadModel == parts[0]).ToArray();
            return read.Length == 1 ? Members(Resolve(read[0].ReadModel, ["ReadModel"], use), parts[1]) : [];
        }
        var trigger = Parents(use).Select(entry => entry.Node).OfType<ReactionTriggerSyntax>().FirstOrDefault();
        if (trigger?.Source is NamedTriggerSourceSyntax named) return Members(Resolve(named.Name, ["Event"], use), path);
        if (Owner(use)?.Syntax is ReactionSyntax reaction)
        {
            return Members(reaction.Triggers.Select(clause => clause.Source).OfType<NamedTriggerSourceSyntax>().SelectMany(source => Resolve(source.Name, ["Event"], use)), path);
        }
        var policy = Owner(use);
        if (policy?.Syntax is PolicySyntax)
        {
            var callers = _index.Incoming(policy).Where(reference => reference.Reference.Role == "authorizes")
                .SelectMany(reference => _index.Declarations.Where(declaration => declaration.Owner == reference.Reference.Owner && declaration.Kind == "Command"));
            return Members(callers, path.Replace("$context.command.", string.Empty, StringComparison.Ordinal));
        }
        return [];
    }
}
