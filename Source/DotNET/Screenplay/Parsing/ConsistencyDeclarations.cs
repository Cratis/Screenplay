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
    public EventSyntax? Event(string name, DeclarationScope scope) => Resolve(
        name,
        scope,
        slice => slice.Events.GroupBy(@event => @event.Name, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(@event => @event.Generation).First()),
        node => node.Name)?.Node;

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
    /// Gets whether the application itself declares something an import could stand in for under a name - an
    /// event, a command, a read model, a concept or a type.
    /// </summary>
    /// <param name="name">The short name.</param>
    /// <returns>Whether any such declaration carries the name.</returns>
    public bool Declares(string name) =>
        slices.Any(entry => entry.Slice.Events.Any(@event => @event.Name == name) ||
            entry.Slice.Commands.Any(command => command.Name == name) ||
            ViewNames(entry.Slice).Contains(name, StringComparer.Ordinal)) ||
        application.Concepts.Any(concept => concept.Name == name) ||
        (application.Types ?? []).Any(type => type.Name == name);

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
    /// <remarks>
    /// A path that continues past a primitive, concept or enum field is provably absent - a concept wraps exactly
    /// one value, so there is nothing below it to name. A path through an undeclared, imported or ambiguous type
    /// stays unknown. A collection or optional composite field is addressed element-wise: <c>rows.note</c> names
    /// the <c>note</c> of each <c>Row</c> in <c>rows Row[]</c>.
    /// </remarks>
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

            if (IsScalar(property.Type.Name))
            {
                missing = true;
                return null;
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

    // A concept wraps exactly one primitive, so nothing sits below a primitive, a concept or an enum. A name that
    // is also a composite type is ambiguous and stays unknown.
    bool IsScalar(string type) =>
        ConceptSyntax.PrimitiveTypes.Contains(type, StringComparer.Ordinal) ||
        (application.Concepts.Any(concept => concept.Name == type) && !(application.Types ?? []).Any(composite => composite.Name == type));

    ReferenceResolver.Resolution ResolveDeclaration(string name, DeclarationScope scope, IReadOnlyList<Declaration> declarations)
    {
        // A declaration of this application is the declaration whether or not an import also names it - an
        // import that repeats a declared name has no effect and is reported on its own, so it must never
        // change what the checks see. Only a name nothing here declares falls to the imports.
        var resolution = ReferenceResolver.Resolve(name, scope, declarations);
        if (!resolution.IsUnresolved)
        {
            return resolution;
        }

        var imports = application.Imports.Where(import => import.Name == name).ToList();
        return imports.Count == 1 ? ReferenceResolver.Resolve(imports[0].QualifiedName, scope, declarations) : new(null, []);
    }
}
