// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Resolves the declarations whose shapes are known to syntax-level consistency checks.
/// </summary>
/// <param name="application">The complete application.</param>
/// <param name="slices">The slices and their declaration scopes.</param>
internal sealed class ConsistencyDeclarations(ApplicationSyntax application, IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> slices)
{
    /// <summary>
    /// Gets the scoped slices.
    /// </summary>
    public IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> Slices => slices;

    /// <summary>
    /// Resolves one declaration without guessing between ambiguous candidates.
    /// </summary>
    /// <typeparam name="T">The declaration syntax.</typeparam>
    /// <param name="name">The referenced name.</param>
    /// <param name="scope">The referring scope.</param>
    /// <param name="select">The declarations in a slice.</param>
    /// <param name="nameOf">The name of a declaration.</param>
    /// <returns>The resolved node and its own scope, or null.</returns>
    public (T Node, DeclarationScope Scope)? Resolve<T>(string name, DeclarationScope scope, Func<SliceSyntax, IEnumerable<T>> select, Func<T, string> nameOf)
    {
        var entries = slices.SelectMany(entry => select(entry.Slice).Select(node => (Node: node, Declaration: new Declaration(nameOf(node), entry.Scope)))).ToList();
        var resolution = ResolveDeclaration(name, scope, [.. entries.Select(entry => entry.Declaration)]);
        if (resolution.Resolved is not { } resolved)
        {
            return null;
        }

        var match = entries.First(entry => entry.Declaration == resolved);
        return (match.Node, match.Declaration.Scope);
    }

    /// <summary>
    /// Resolves an event in the caller's scope.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="scope">The referring scope.</param>
    /// <returns>The declared event, or null for an unknown or ambiguous shape.</returns>
    public EventSyntax? Event(string name, DeclarationScope scope) => Resolve(name, scope, slice => slice.Events, node => node.Name)?.Node;

    /// <summary>
    /// Resolves a view identity, including projection aliases and variant names.
    /// </summary>
    /// <param name="name">The view name.</param>
    /// <param name="scope">The referring scope.</param>
    /// <returns>The resolved declaration, or null.</returns>
    public Declaration? View(string name, DeclarationScope scope)
    {
        var declarations = slices.SelectMany(entry => ViewNames(entry.Slice).Distinct(StringComparer.Ordinal)
            .Select(view => new Declaration(view, entry.Scope))).ToList();
        return ResolveDeclaration(name, scope, declarations).Resolved;
    }

    /// <summary>
    /// Resolves the explicitly declared shape of a read model.
    /// </summary>
    /// <param name="name">The read model name.</param>
    /// <param name="scope">The referring scope.</param>
    /// <returns>The properties, or null when the shape is unknown.</returns>
    public IEnumerable<PropertySyntax>? ViewProperties(string name, DeclarationScope scope) =>
        Resolve(name, scope, slice => slice.ReadModels ?? [], node => node.Name)?.Node.Properties;

    /// <summary>
    /// Resolves a composite type's properties.
    /// </summary>
    /// <param name="name">The type name.</param>
    /// <returns>The properties, or null for an unknown or ambiguous type.</returns>
    public IEnumerable<PropertySyntax>? TypeProperties(string name)
    {
        var matches = (application.Types ?? []).Where(type => type.Name == name).ToList();
        return matches.Count == 1 ? matches[0].Properties : null;
    }

    /// <summary>
    /// Resolves a property's own type, following declared composite paths.
    /// </summary>
    /// <param name="properties">The containing declaration's properties.</param>
    /// <param name="path">The property path.</param>
    /// <param name="missing">Whether the path is provably absent rather than unknown.</param>
    /// <returns>The property if it can be resolved.</returns>
    public PropertySyntax? Property(IEnumerable<PropertySyntax>? properties, string path, out bool missing)
    {
        missing = false;
        var segments = path.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            if (properties is null)
            {
                return null;
            }

            var matches = properties.Where(property => property.Name == segments[index]).ToList();
            if (matches.Count != 1)
            {
                missing = matches.Count == 0;
                return null;
            }

            var property = matches[0];
            if (index == segments.Length - 1)
            {
                return property;
            }

            properties = TypeProperties(property.Type.Name);
        }

        return null;
    }

    /// <summary>
    /// Resolves the enum concept of a field from that field's declaration.
    /// </summary>
    /// <param name="type">The field's type.</param>
    /// <returns>The enum, or null.</returns>
    public ConceptSyntax? Enumeration(TypeRefSyntax type)
    {
        var matches = application.Concepts.Where(concept => concept.Name == type.Name && concept.IsEnum).ToList();
        return !type.IsCollection && matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Compares nominal types when both are known, never equating distinct concepts by their primitive.
    /// </summary>
    /// <param name="source">The supplied type.</param>
    /// <param name="target">The accepted type.</param>
    /// <returns>Compatibility, or null when either type is unknown.</returns>
    public bool? Compatible(TypeRefSyntax source, TypeRefSyntax target)
    {
        var known = ConceptSyntax.PrimitiveTypes.Concat(application.Concepts.Select(concept => concept.Name))
            .Concat((application.Types ?? []).Select(type => type.Name)).ToHashSet(StringComparer.Ordinal);
        if (!known.Contains(source.Name) || !known.Contains(target.Name))
        {
            return null;
        }

        return source.Name == target.Name && source.IsCollection == target.IsCollection && (!source.IsOptional || target.IsOptional);
    }

    static IEnumerable<string> ViewNames(SliceSyntax slice) =>
        (slice.ReadModels ?? []).Select(model => model.Name)
            .Concat((slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Concat(slice.Projections.SelectMany(projection => projection.Blocks.OfType<ProjectionVariantSyntax>().Any()
                ? projection.Blocks.OfType<ProjectionVariantSyntax>().Select(variant => variant.Name)
                : [projection.ReadModel ?? projection.Name]));

    ReferenceResolver.Resolution ResolveDeclaration(string name, DeclarationScope scope, IReadOnlyList<Declaration> declarations)
    {
        var local = declarations.Where(declaration => declaration.Name == name && declaration.Scope.SharesPrefixWith(scope, scope.Depth)).ToList();
        var imports = application.Imports.Where(import => import.Name == name).ToList();
        if (local.Count == 0 && imports.Count > 0)
        {
            // An import names its target explicitly; an unrelated declaration with the same short name
            // elsewhere in this document must not supply the imported artifact's shape.
            return imports.Count == 1 ? ReferenceResolver.Resolve(imports[0].QualifiedName, scope, declarations) : new(null, []);
        }

        return ReferenceResolver.Resolve(name, scope, declarations);
    }
}
