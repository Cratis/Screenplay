// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines declaration resolution outcomes for authoring, not executable binding.
/// </summary>
public enum AuthoringProductionKind
{
    /// <summary>
    /// No explicit declaration resolves.
    /// </summary>
    Unresolved,

    /// <summary>
    /// An event declaration resolves.
    /// </summary>
    Event,

    /// <summary>
    /// An operation declaration resolves.
    /// </summary>
    Operation,

    /// <summary>
    /// More than one declaration wins.
    /// </summary>
    Ambiguous
}

/// <summary>
/// An explicit declaration with its kind and complete authoring scope.
/// </summary>
/// <param name="Kind">The declared kind.</param>
/// <param name="Name">The declared name.</param>
/// <param name="Scope">The module, feature and slice path.</param>
/// <param name="Node">The declaration syntax.</param>
public sealed record AuthoringProductionDeclaration(AuthoringProductionKind Kind, string Name, IReadOnlyList<string> Scope, SyntaxNode Node);

/// <summary>
/// The result of resolving a production target without guessing its kind.
/// </summary>
/// <param name="Kind">The outcome.</param>
/// <param name="Declaration">The resolved declaration, if unique.</param>
/// <param name="Candidates">The ambiguous candidates.</param>
public sealed record AuthoringProductionResolution(AuthoringProductionKind Kind, AuthoringProductionDeclaration? Declaration, IReadOnlyList<AuthoringProductionDeclaration> Candidates);

/// <summary>
/// Resolves authoring productions while leaving legacy event and ESM resolution unchanged.
/// </summary>
public sealed class AuthoringProductionResolver
{
    readonly ReferenceDeclarationIndex _candidates;
    readonly Dictionary<Declaration, AuthoringProductionDeclaration> _occurrences = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Scope, string Reference), AuthoringProductionResolution> _cache = [];
    readonly Dictionary<SliceSyntax, DeclarationScope> _scopes = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Initializes an explicit declaration inventory.
    /// </summary>
    /// <param name="application">The assembled application.</param>
    public AuthoringProductionResolver(ApplicationSyntax application)
        : this(Inventory(application))
    {
        foreach (var (slice, scope) in application.Modules.SelectMany(module => Features(module.Features, [module.Name]))) _scopes.Add(slice, scope);
    }

    /// <summary>
    /// Initializes an explicit candidate view. The caller retains physical collisions and
    /// selects valid logical event generations before supplying the view.
    /// </summary>
    /// <param name="declarations">The complete declaration candidates.</param>
    public AuthoringProductionResolver(IEnumerable<AuthoringProductionDeclaration> declarations)
    {
        Declarations = [.. declarations];
        foreach (var declaration in Declarations) _occurrences.Add(new(declaration.Name, new(declaration.Scope)), declaration);
        _candidates = new(_occurrences.Keys);
    }

    /// <summary>
    /// Gets declarations with their complete scope.
    /// </summary>
    public IReadOnlyList<AuthoringProductionDeclaration> Declarations { get; }

    /// <summary>
    /// Resolves a bare or qualified reference from its owning slice.
    /// </summary>
    /// <param name="reference">The authored reference.</param>
    /// <param name="slice">The referring slice.</param>
    /// <returns>The resolved kind, declaration and ambiguity evidence.</returns>
    public AuthoringProductionResolution Resolve(string reference, SliceSyntax slice)
    {
        if (!_scopes.TryGetValue(slice, out var scope)) return new(AuthoringProductionKind.Unresolved, null, []);

        return Resolve(reference, scope.Segments);
    }

    /// <summary>
    /// Resolves against the explicit candidate view from a known complete authoring scope.
    /// </summary>
    /// <param name="reference">The authored bare or qualified reference.</param>
    /// <param name="scope">The complete referring scope.</param>
    /// <returns>The resolved kind, declaration and ambiguity evidence.</returns>
    public AuthoringProductionResolution Resolve(string reference, IReadOnlyList<string> scope)
    {
        var key = (ReferenceDeclarationIndex.ScopeKey(scope), reference);
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var result = ReferenceResolver.Resolve(reference, new DeclarationScope(scope), _candidates);
        if (result.Resolved is { } resolved)
        {
            var declaration = _occurrences[resolved];
            var found = new AuthoringProductionResolution(declaration.Kind, declaration, []);
            _cache[key] = found;
            return found;
        }

        var outcome = new AuthoringProductionResolution(
            result.IsUnresolved ? AuthoringProductionKind.Unresolved : AuthoringProductionKind.Ambiguous,
            null,
            [.. result.Ambiguous.Select(candidate => _occurrences[candidate])]);
        _cache[key] = outcome;

        return outcome;
    }

    /// <summary>
    /// Gets whether a production resolves to an explicitly declared operation.
    /// </summary>
    /// <param name="production">The ordered production.</param>
    /// <param name="slice">The owning slice.</param>
    /// <returns>Whether its target is an operation.</returns>
    public bool IsOperation(ProducesSyntax production, SliceSyntax slice) => production.InlineOperation is not null || Resolve(production.Event, slice).Kind == AuthoringProductionKind.Operation;

    /// <summary>
    /// Gets whether event-only consumers may inspect a production without guessing its declared kind.
    /// Unresolved legacy references retain their existing event diagnostics and repair availability.
    /// </summary>
    /// <param name="production">The ordered production.</param>
    /// <param name="slice">The owning slice.</param>
    /// <returns>Whether this is an event or a legacy unresolved event reference, not an operation or a mixed-kind ambiguity.</returns>
    public bool IsEventProduction(ProducesSyntax production, SliceSyntax slice)
    {
        if (production.InlineOperation is not null) return false;
        var resolution = Resolve(production.Event, slice);

        return resolution.Kind != AuthoringProductionKind.Operation &&
            !(resolution.Kind == AuthoringProductionKind.Ambiguous && resolution.Candidates.Any(candidate => candidate.Kind == AuthoringProductionKind.Operation));
    }

    static IEnumerable<AuthoringProductionDeclaration> Inventory(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => Features(module.Features, [module.Name])).SelectMany(entry => EventDeclarations.In(entry.Slice)
            .GroupBy(node => node.Name, StringComparer.Ordinal)
            .SelectMany(group => (group.Select(node => node.Generation).Distinct().Count() != group.Count()
                ? group.AsEnumerable() : group.OrderByDescending(node => node.Generation).Take(1))
                .Select(node => new AuthoringProductionDeclaration(AuthoringProductionKind.Event, group.Key, entry.Scope.Segments, node)))
            .Concat(OperationDeclarations.In(entry.Slice).Select(node => new AuthoringProductionDeclaration(AuthoringProductionKind.Operation, node.Name, entry.Scope.Segments, node))));

    static IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> Features(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> outer)
    {
        foreach (var feature in features)
        {
            string[] scope = [.. outer, feature.Name];
            foreach (var slice in feature.Slices) yield return (slice, new([.. scope, slice.Name]));
            foreach (var nested in Features(feature.Features, scope)) yield return nested;
        }
    }
}
