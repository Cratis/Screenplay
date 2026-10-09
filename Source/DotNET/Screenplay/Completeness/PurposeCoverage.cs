// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

/// <summary>
/// Resolves processing-purpose coverage and value classifications through composite types.
/// </summary>
public static class PurposeCoverage
{
    /// <summary>
    /// Enumerates slices with the union of directly and ancestrally referenced purposes.
    /// </summary>
    /// <param name="application">The merged application.</param>
    /// <returns>Each slice and its effective purpose references.</returns>
    public static IEnumerable<(SliceSyntax Slice, IReadOnlyList<PurposeReferenceSyntax> Purposes)> Slices(ApplicationSyntax application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Slices(feature, module.Purposes)));

    /// <summary>
    /// Finds concepts reachable through the value shapes declared in a slice.
    /// </summary>
    /// <param name="application">The merged application.</param>
    /// <param name="slice">The slice to inspect.</param>
    /// <returns>The distinct concepts reachable through command, event, read-model and query fields.</returns>
    public static IEnumerable<ConceptSyntax> Concepts(ApplicationSyntax application, SliceSyntax slice)
    {
        var visitor = new FieldTypes();
        foreach (var command in slice.Commands) visitor.VisitCommand(command);
        foreach (var declaration in slice.Events) visitor.VisitEvent(declaration);
        foreach (var readModel in slice.ReadModels ?? []) visitor.VisitReadModel(readModel);
        foreach (var query in slice.Queries) visitor.VisitQuery(query);
        var scopes = ScreenplayValidator.ScopedSlices(application).ToArray();
        var declarations = new ConsistencyDeclarations(application, scopes);
        foreach (var entry in scopes.Where(entry => ReferenceEquals(entry.Slice, slice)))
        {
            foreach (var property in slice.Queries.SelectMany(query => declarations.ViewProperties(query.ReturnType.Name, entry.Scope) ?? [])) visitor.VisitProperty(property);
        }

        var types = (application.Types ?? []).GroupBy(type => type.Name).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var names = visitor.Names;
        var pending = new Queue<string>(names);
        while (pending.TryDequeue(out var name))
        {
            if (!types.TryGetValue(name, out var type)) continue;
            foreach (var field in type.Properties)
            {
                if (names.Add(field.Type.Name)) pending.Enqueue(field.Type.Name);
            }
        }

        return application.Concepts.Where(concept => names.Contains(concept.Name));
    }

    static IEnumerable<(SliceSyntax Slice, IReadOnlyList<PurposeReferenceSyntax> Purposes)> Slices(FeatureSyntax feature, IEnumerable<PurposeReferenceSyntax> inherited)
    {
        var purposes = inherited.Concat(feature.Purposes).DistinctBy(purpose => purpose.Name).ToArray();
        foreach (var slice in feature.Slices) yield return (slice, purposes.Concat(slice.Purposes).DistinctBy(purpose => purpose.Name).ToArray());
        foreach (var nested in feature.Features.SelectMany(child => Slices(child, purposes))) yield return nested;
    }

    sealed class FieldTypes : ScreenplaySyntaxWalker
    {
        internal HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        public override void VisitTypeRef(TypeRefSyntax syntax) => Names.Add(syntax.Name);
    }
}
