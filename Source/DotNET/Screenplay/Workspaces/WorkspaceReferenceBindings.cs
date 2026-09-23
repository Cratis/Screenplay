// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed record WorkspaceReferenceDeclaration(string Key, string Name, DeclarationScope Scope, WorkspaceReferenceDomain Domain, WorkspaceSyntaxEntry? Entry, string? Owner = null);
sealed record WorkspaceReferenceBinding(WorkspaceReferenceMember Reference, WorkspaceReferenceDeclaration? Target, string Outcome);

sealed class WorkspaceReferenceBindings
{
    readonly WorkspaceSyntaxIndex _index;
    readonly WorkspaceReferenceDeclaration[] _declarations;
    readonly Dictionary<(WorkspaceReferenceDomain Domain, string Name), WorkspaceReferenceDeclaration[]> _byName;
    readonly Dictionary<string, string[]> _imports;

    internal WorkspaceReferenceBindings(WorkspaceSyntaxIndex index)
    {
        _index = index;
        _declarations = [.. Declarations(index)];
        _byName = _declarations.GroupBy(declaration => (declaration.Domain, declaration.Name)).ToDictionary(group => group.Key, group => group.ToArray());
        _imports = index.Entries.Select(entry => entry.Node).OfType<ImportSyntax>().GroupBy(import => import.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(import => import.QualifiedName).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        Bindings = [.. WorkspaceReferenceMembers.All(index).Select(Bind)];
    }

    internal WorkspaceReferenceBinding[] Bindings { get; }

    internal static DeclarationScope Scope(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        var names = new List<string>();
        var current = entry.Parent is { } parent ? index.Find(parent) : null;
        while (current is not null)
        {
            if (current.Node is ModuleSyntax or FeatureSyntax or SliceSyntax)
            {
                names.Add(Name(current.Node)!);
            }

            current = current.Parent is { } ancestor ? index.Find(ancestor) : null;
        }

        names.Reverse();
        return new(names);
    }

    internal static string? Name(SyntaxNode node) => node.GetType().GetProperty("Name")?.GetValue(node) as string;

    internal static string Key(WorkspaceSyntaxEntry entry) => $"{entry.Handle.Document}:{entry.Handle.Path}";

    internal void RequireNoCollisions()
    {
        if (Bindings.Any(binding => binding.Outcome == "compiler-resolution-disagreement"))
        {
            throw new InvalidWorkspaceAuthoring("Cannot prove a rename where syntax validation and executable binding use different reference resolution domains.");
        }

        foreach (var group in _declarations.GroupBy(declaration => (declaration.Domain, declaration.Name)))
        {
            // ESM uses global short-name dictionaries for these domains. Accepting a local winner here
            // would claim safety the executable binder cannot provide.
            if (group.Select(declaration => declaration.Key).Distinct(StringComparer.Ordinal).Count() > 1 &&
                group.Key.Domain is WorkspaceReferenceDomain.Type or WorkspaceReferenceDomain.Event or WorkspaceReferenceDomain.Command or WorkspaceReferenceDomain.View or WorkspaceReferenceDomain.Query)
            {
                throw new InvalidWorkspaceAuthoring($"Cannot prove a safe rename: colliding {group.Key.Domain} name '{group.Key.Name}' across compiler resolution domains.");
            }
        }
    }

    static IEnumerable<WorkspaceReferenceDeclaration> Declarations(WorkspaceSyntaxIndex index)
    {
        foreach (var primitive in ConceptSyntax.PrimitiveTypes)
        {
            yield return new($"primitive:{primitive}", primitive, new([]), WorkspaceReferenceDomain.Type, null);
        }

        foreach (var entry in index.Entries)
        {
            WorkspaceReferenceDomain? domain = entry.Node switch
            {
                ConceptSyntax or TypeSyntax => WorkspaceReferenceDomain.Type,
                EventSyntax => WorkspaceReferenceDomain.Event,
                CommandSyntax => WorkspaceReferenceDomain.Command,
                ReadModelSyntax => WorkspaceReferenceDomain.View,
                QuerySyntax => WorkspaceReferenceDomain.Query,
                ScreenSyntax => WorkspaceReferenceDomain.Screen,
                PolicySyntax => WorkspaceReferenceDomain.Policy,
                TriggerSyntax => WorkspaceReferenceDomain.Trigger,
                PropertySyntax when entry.Parent is { } parent && index.Find(parent)?.Node is TypeSyntax => WorkspaceReferenceDomain.Property,
                _ => null
            };
            if (domain is { } actual)
            {
                var owner = actual == WorkspaceReferenceDomain.Property ? ((TypeSyntax)index.Find(entry.Parent!)!.Node).Name : null;
                yield return new(Key(entry), Name(entry.Node)!, Scope(entry, index), actual, entry, owner);
            }
        }

        var declaredViews = index.Entries.Where(entry => entry.Node is ReadModelSyntax)
            .Select(entry => Name(entry.Node)!).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in index.Entries)
        {
            string[] names = entry.Node switch
            {
                ProjectionSyntax projection when !projection.Blocks.OfType<ProjectionVariantSyntax>().Any() => [projection.ReadModel ?? projection.Name],
                ProjectionVariantSyntax variant => [variant.Name],
                ReducerSyntax reducer => [reducer.ReadModel],
                _ => []
            };
            foreach (var name in names)
            {
                var scope = Scope(entry, index);
                if (!declaredViews.Contains(name))
                {
                    yield return new(Key(entry), name, scope, WorkspaceReferenceDomain.View, entry);
                }
            }
        }
    }

    static bool AdmitsImports(WorkspaceReferenceMember reference) => reference.Domain switch
    {
        WorkspaceReferenceDomain.Type or WorkspaceReferenceDomain.Event or WorkspaceReferenceDomain.View or WorkspaceReferenceDomain.Trigger => true,
        WorkspaceReferenceDomain.Command => reference.Entry.Node is InvokesSyntax,
        _ => false
    };

    WorkspaceReferenceBinding Bind(WorkspaceReferenceMember reference)
    {
        var domain = reference.Domain;
        if (domain == WorkspaceReferenceDomain.Property)
        {
            var properties = (_byName.GetValueOrDefault((domain, reference.Text)) ?? [])
                .Where(declaration => declaration.Owner == reference.Owner).ToArray();
            return properties.Length == 1
                ? new(reference, properties[0], "resolved")
                : new(reference, null, properties.Length == 0 ? "unresolved" : "ambiguous");
        }

        if (reference.Text.Contains('.') && (domain is WorkspaceReferenceDomain.Type or WorkspaceReferenceDomain.Policy or WorkspaceReferenceDomain.Trigger ||
            (domain == WorkspaceReferenceDomain.Event && reference.Entry.Node is not SpecificationEventSyntax) ||
            reference.Entry.Node is InvokesSyntax or ReadsSyntax or ProjectionSyntax or ReducerSyntax))
        {
            return new(reference, null, "compiler-resolution-disagreement");
        }

        var name = reference.Text.Split('.')[^1];
        var declarations = _byName.GetValueOrDefault((domain, name)) ?? [];
        var declaredTrigger = domain == WorkspaceReferenceDomain.Trigger && declarations.Length > 0;

        // The compiler resolves declared triggers before considering events, irrespective of scope.
        if (domain == WorkspaceReferenceDomain.Trigger && declarations.Length == 0)
        {
            declarations = _byName.GetValueOrDefault((WorkspaceReferenceDomain.Event, name)) ?? [];
        }

        var scope = Scope(reference.Entry, _index);
        var imports = AdmitsImports(reference) ? _imports.GetValueOrDefault(reference.Text) ?? [] : [];
        if (imports.Length > 0 && !declarations.Any(declaration => declaration.Name == reference.Text &&
            (declaredTrigger || domain == WorkspaceReferenceDomain.Type || declaration.Scope.SharesPrefixWith(scope, scope.Depth))))
        {
            return imports.Length == 1
                ? new(reference, new($"import:{imports[0]}", reference.Text, new([]), domain, null), "resolved")
                : new(reference, null, "ambiguous-import");
        }

        var resolution = ReferenceResolver.Resolve(reference.Text, scope, [.. declarations.Select(declaration => new Declaration(declaration.Name, declaration.Scope))]);
        if (resolution.Resolved is not { } resolved)
        {
            var disagreement = resolution.IsUnresolved && declarations.Length > 0 && reference.Text.Contains('.') &&
                domain is WorkspaceReferenceDomain.Event or WorkspaceReferenceDomain.Command or WorkspaceReferenceDomain.View or WorkspaceReferenceDomain.Query;
            var outcome = resolution.IsUnresolved ? "unresolved" : "ambiguous";
            return new(reference, null, disagreement ? "compiler-resolution-disagreement" : outcome);
        }

        var matches = declarations.Where(declaration => declaration.Name == resolved.Name && declaration.Scope.Segments.SequenceEqual(resolved.Scope.Segments)).ToArray();
        if (matches.Length != 1)
        {
            return new(reference, null, "ambiguous");
        }

        if (domain is WorkspaceReferenceDomain.Type or WorkspaceReferenceDomain.Event or WorkspaceReferenceDomain.Command or WorkspaceReferenceDomain.View or WorkspaceReferenceDomain.Query &&
            declarations.Count(declaration => declaration.Name == resolved.Name) != 1)
        {
            return new(reference, null, "compiler-resolution-disagreement");
        }

        return new(reference, matches[0], "resolved");
    }
}
