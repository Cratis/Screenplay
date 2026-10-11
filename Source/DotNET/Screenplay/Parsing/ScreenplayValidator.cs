// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates cross references in a parsed document - policies referenced by <c>authorize</c> and personas,
/// events referenced by reactions, <c>produces</c>, constraints and <c>seed</c> blocks, and the types
/// referenced by properties - and that <c>concurrency</c> and <c>seed</c> blocks are not empty and
/// <c>authentication</c> provider and <c>type</c> names are unique.
/// </summary>
internal static partial class ScreenplayValidator
{
    static readonly IReadOnlySet<string> _noNames = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Validates an application and reports diagnostics for unknown references.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="allowUnresolvedPersonaPolicies">Whether draft authoring may retain unresolved persona references as warnings.</param>
    /// <returns>The application with repeated dependency targets kept once.</returns>
    public static ApplicationSyntax Validate(ApplicationSyntax application, ParserContext context, bool allowUnresolvedPersonaPolicies = false)
    {
        foreach (var error in SourceNumericModes.Errors(application)) context.Add(error);
        new PublicEventMetadataValidator(context).VisitApplication(application);
        PurposeValidator.Validate(application, context);
        var authored = application;
        var expansion = SpecificationExamples.Expand(application);
        application = EffectiveApplication(application, expansion);

        var slices = application.Modules
            .SelectMany(module => module.Features.SelectMany(AllFeatures))
            .SelectMany(feature => feature.Slices)
            .ToList();
        var names = DocumentNames.Of(application, slices);

        ValidateDeclarations(application, slices, names, allowUnresolvedPersonaPolicies, context);
        ValidateSlices(application, slices, names, context);
        ValidateScopedReferences(application, expansion, names, context);
        ValidatePresentation(application, context);
        foreach (var diagnostic in expansion.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.UnsynthesizablePersonaCaller && !context.Diagnostics.Contains(diagnostic))) context.Add(diagnostic);

        authored = DeclaredDependencyTargets.Validate(authored, context);
        DeclaredDependencies.Validate(authored, context);

        return authored;
    }

    /// <summary>
    /// Yields every slice with the scope it sits in, outermost segment first.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to walk.</param>
    /// <returns>Each slice and where it sits.</returns>
    internal static IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> ScopedSlices(ApplicationSyntax application)
    {
        foreach (var module in application.Modules)
        {
            foreach (var entry in ScopedSlicesIn(module.Features, [module.Name]))
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Gets the names of the read model(s) a projection builds. A projection with <c>variant</c> blocks builds
    /// one read model per variant, named after the variant, rather than one named after the projection itself -
    /// the projection's own name identifies the shared logical identity, not a read model.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> to inspect.</param>
    /// <returns>The read model name(s) the projection builds.</returns>
    internal static IEnumerable<string> ProjectionBuiltReadModelNames(ProjectionSyntax projection)
    {
        var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToList();
        return variants.Count > 0 ? variants.Select(variant => variant.Name) : [projection.ReadModel ?? projection.Name];
    }

    /// <summary>
    /// Yields every feature in a module's feature tree with the scope it sits in, at any nesting depth.
    /// </summary>
    /// <param name="features">The top level <see cref="FeatureSyntax">features</see> to walk.</param>
    /// <param name="path">The scope path leading to <paramref name="features"/>, outermost first.</param>
    /// <returns>Each feature and where it sits.</returns>
    internal static IEnumerable<(FeatureSyntax Feature, DeclarationScope Scope)> AllFeaturesScoped(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path)
    {
        foreach (var feature in features)
        {
            List<string> featurePath = [.. path, feature.Name];
            yield return (feature, new DeclarationScope(featurePath));

            foreach (var nested in AllFeaturesScoped(feature.Features, featurePath))
            {
                yield return nested;
            }
        }
    }

    internal static void Report(
        string reference,
        DeclarationScope from,
        IReadOnlyList<Declaration> declarations,
        string unknownCode,
        string kind,
        SourceLocation location,
        ParserContext context)
    {
        var resolution = ReferenceResolver.Resolve(reference, from, declarations);
        if (resolution.Resolved is not null)
        {
            return;
        }

        if (resolution.Ambiguous.Count > 0)
        {
            var where = string.Join(", ", resolution.Ambiguous.Select(candidate => string.Join('.', candidate.Scope.Segments)));
            context.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Ambiguous {kind} '{reference}' - it matches {resolution.Ambiguous.Count} declarations equally well ({where}); qualify it to say which",
                location);
            return;
        }

        context.Warning(unknownCode, $"Unknown {kind} '{reference}' - nothing in scope declares it", location);
    }

    static ApplicationSyntax EffectiveApplication(ApplicationSyntax application, EffectiveSpecificationApplication expansion) =>
        expansion.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code != DiagnosticCodes.UnsynthesizablePersonaCaller)
            ? application
            : expansion.Application;

    static void ValidateDeclarations(
        ApplicationSyntax application,
        List<SliceSyntax> slices,
        DocumentNames names,
        bool allowUnresolvedPersonaPolicies,
        ParserContext context)
    {
        ValidateAuthorizations(application, names.Policies, context);
        ValidateReadModels(slices, names.ReadModels, names.Events, context);
        ValidateTypes(application, names.Types, context);
        IdentityValidator.Validate(application, names.Types.Union(names.ReadModels).ToHashSet(StringComparer.Ordinal), context);

        ValidatePersonaPolicies(application, names.Policies, allowUnresolvedPersonaPolicies, context);
        ValidateDuplicateProviders(application, context);
        ValidateSeeds(application, names.Events, context);
    }

    static void ValidateSlices(ApplicationSyntax application, List<SliceSyntax> slices, DocumentNames names, ParserContext context)
    {
        // The command names are collected before the triggers and events below, as they always were.
        _ = names.Commands;
        var declaredTriggers = (application.Triggers ?? []).ToDictionary(trigger => trigger.Name, StringComparer.Ordinal);
        var eventsByName = slices.SelectMany(EventDeclarations.In)
            .GroupBy(@event => @event.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(@event => @event.Generation).First(), StringComparer.Ordinal);

        var catalog = new TriggerCatalog(declaredTriggers, eventsByName);
        var eventDeclarations = slices.SelectMany(EventDeclarations.In).ToLookup(@event => @event.Name, StringComparer.Ordinal);
        var productionResolver = new AuthoringProductionResolver(application);
        InlineEventValidator.Validate(application, slices, context);
        foreach (var slice in slices)
        {
            ValidateEventGenerations(slice, context);
            ValidateConstraintProperties(slice, eventDeclarations, context);
            ValidateSlice(slice, names, productionResolver, context);
            ValidateReactionConsequences(slice, names, productionResolver, context);
            ValidateReactionTriggers(slice, names, catalog, context);
        }
    }

    static void ValidateScopedReferences(ApplicationSyntax application, EffectiveSpecificationApplication expansion, DocumentNames names, ParserContext context)
    {
        var scopedSlices = ScopedSlices(application).ToList();
        var declarations = new ConsistencyDeclarations(application, scopedSlices);
        PublicEventUsageValidator.Validate(application, context);
        ValidateAdditionalEventReferences(application, declarations, names.Events, context);
        RunConsistencyValidators(application, declarations, expansion, context);
        var knownQueries = scopedSlices.SelectMany(entry => entry.Slice.Queries.Select(query => new Declaration(query.Name, entry.Scope))).ToList();
        var knownScreenDeclarations = scopedSlices.SelectMany(entry => entry.Slice.Screens.Select(screen => new Declaration(screen.Name, entry.Scope))).ToList();
        ValidateSpecificationQueries(scopedSlices, knownQueries, context);

        var (knownCommandDeclarations, commandsByDeclaration) = CommandDeclarations(scopedSlices);
        var references = new InteractionReferences(knownCommandDeclarations, knownScreenDeclarations, knownQueries, _noNames, names.Events, _noNames);
        ValidateScreenReferences(scopedSlices, references, context);
        GuardedActionValidator.Validate(application, declarations, context);

        // The dialog templates and triggers are only collected once the checks above have run.
        references = references with
        {
            DialogTemplates = application.Modules.SelectMany(module => module.DialogTemplates ?? []).Select(template => template.Name).ToHashSet(StringComparer.Ordinal),
            Triggers = (application.Triggers ?? []).Select(trigger => trigger.Name).ToHashSet(StringComparer.Ordinal)
        };
        new InteractionReferenceValidator(application, references, context).Validate(scopedSlices);
        ValidateFormReferences(application, commandsByDeclaration, references, context);
        ValidateContributions(application, knownScreenDeclarations, context);
    }

    static void ValidatePresentation(ApplicationSyntax application, ParserContext context)
    {
        ValidateThemes(application, context);
        ValidateProfileLayouts(application, context);
        ValidateArrangements(application, context);
        CompositionValidator.Validate(application, context);
    }

    static IEnumerable<FeatureSyntax> AllFeatures(FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    static IEnumerable<(SliceSyntax Slice, DeclarationScope Scope)> ScopedSlicesIn(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> path)
    {
        foreach (var feature in features)
        {
            List<string> featurePath = [.. path, feature.Name];
            foreach (var slice in feature.Slices)
            {
                yield return (slice, new DeclarationScope([.. featurePath, slice.Name]));
            }

            foreach (var nested in ScopedSlicesIn(feature.Features, featurePath))
            {
                yield return nested;
            }
        }
    }

    internal sealed record TriggerCatalog(Dictionary<string, TriggerSyntax> Declared, Dictionary<string, EventSyntax> EventsByName);
}
