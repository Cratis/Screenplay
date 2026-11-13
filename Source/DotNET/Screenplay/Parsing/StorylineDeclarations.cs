// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// One declaration a storyline step can name, with the scope it sits in and its node.
/// </summary>
/// <param name="Declaration">The name and scope the reference resolver matches.</param>
/// <param name="Node">The declaring node.</param>
internal sealed record StorylineDeclaration(Declaration Declaration, SyntaxNode Node);

/// <summary>
/// Collects, per <see cref="StorylineElementKind"/>, what a storyline step can name and resolves steps against it.
/// </summary>
internal sealed class StorylineDeclarations
{
    readonly Dictionary<StorylineElementKind, List<StorylineDeclaration>> _byKind;

    StorylineDeclarations(Dictionary<StorylineElementKind, List<StorylineDeclaration>> byKind) => _byKind = byKind;

    /// <summary>
    /// Gets the declarations of one kind, in the shape the reference resolver takes.
    /// </summary>
    /// <param name="kind">The <see cref="StorylineElementKind"/>.</param>
    internal IReadOnlyList<Declaration> this[StorylineElementKind kind] => [.. Of(kind).Select(entry => entry.Declaration)];

    /// <summary>
    /// Collects every module, feature, slice and slice member a storyline can name.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to collect from.</param>
    /// <returns>The collected <see cref="StorylineDeclarations"/>.</returns>
    internal static StorylineDeclarations Collect(ApplicationSyntax application)
    {
        var byKind = Enum.GetValues<StorylineElementKind>().ToDictionary(kind => kind, _ => new List<StorylineDeclaration>());
        void Add(StorylineElementKind kind, string name, IReadOnlyList<string> scope, SyntaxNode node) =>
            byKind[kind].Add(new(new(name, new DeclarationScope(scope)), node));

        foreach (var module in application.Modules)
        {
            Add(StorylineElementKind.Module, module.Name, [], module);
            foreach (var (feature, scope) in ScreenplayValidator.AllFeaturesScoped(module.Features, [module.Name]))
            {
                Add(StorylineElementKind.Feature, feature.Name, [.. scope.Segments.Take(scope.Depth - 1)], feature);
            }
        }

        foreach (var (slice, scope) in ScreenplayValidator.ScopedSlices(application))
        {
            var segments = scope.Segments;
            Add(StorylineElementKind.Slice, slice.Name, [.. segments.Take(segments.Count - 1)], slice);
            foreach (var command in slice.Commands) Add(StorylineElementKind.Command, command.Name, segments, command);

            // An evolved event declares one generation per line; the step names the event, so the newest generation stands for it.
            foreach (var @event in slice.Events.GroupBy(@event => @event.Name, StringComparer.Ordinal).Select(generations => generations.OrderByDescending(@event => @event.Generation).First()))
            {
                Add(StorylineElementKind.Event, @event.Name, segments, @event);
            }

            foreach (var model in slice.ReadModels ?? []) Add(StorylineElementKind.ReadModel, model.Name, segments, model);
            foreach (var query in slice.Queries) Add(StorylineElementKind.Query, query.Name, segments, query);
            foreach (var screen in slice.Screens) Add(StorylineElementKind.Screen, screen.Name, segments, screen);
            foreach (var reaction in slice.Reactions) Add(StorylineElementKind.Reaction, reaction.Name, segments, reaction);
        }

        return new(byKind);
    }

    /// <summary>
    /// Resolves one step to the declaration it names.
    /// </summary>
    /// <param name="step">The <see cref="StorylineStepSyntax"/> to resolve.</param>
    /// <returns>The single matching declaration, or the candidates when the reference is ambiguous; both empty when it names nothing.</returns>
    internal (StorylineDeclaration? Resolved, IReadOnlyList<StorylineDeclaration> Candidates) Resolve(StorylineStepSyntax step)
    {
        var entries = Of(step.ElementKind);
        var resolution = ReferenceResolver.Resolve(step.Target, new DeclarationScope([]), [.. entries.Select(entry => entry.Declaration)]);
        if (resolution.Resolved is { } resolved) return (entries.First(entry => ReferenceEquals(entry.Declaration, resolved)), []);

        return (null, [.. entries.Where(entry => resolution.Ambiguous.Any(candidate => ReferenceEquals(candidate, entry.Declaration)))]);
    }

    List<StorylineDeclaration> Of(StorylineElementKind kind) => _byKind[kind];
}
