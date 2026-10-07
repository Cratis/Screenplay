// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Completeness;

static class ViewBuilders
{
    internal static ReadModelSyntax? ReadModel(string name, DeclarationScope scope, ConsistencyDeclarations declarations) =>
        declarations.Resolve(name, scope, slice => slice.ReadModels ?? [], model => model.Name)?.Node;

    internal static IEnumerable<(ProjectionSyntax Projection, IReadOnlyList<ProjectionBlockSyntax> Blocks, DeclarationScope Scope)> Projections(ReadModelSyntax model, ConsistencyDeclarations declarations)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var projection in slice.Projections)
            {
                var shared = projection.Blocks.Where(block => block is not ProjectionVariantSyntax).ToArray();
                var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToArray();
                if (variants.Length == 0 && ReferenceEquals(ReadModel(projection.ReadModel ?? projection.Name, scope, declarations), model))
                {
                    yield return (projection, shared, scope);
                }

                foreach (var variant in variants.Where(variant => ReferenceEquals(ReadModel(variant.Name, scope, declarations), model)))
                {
                    yield return (projection, [.. shared, .. variant.Blocks], scope);
                }
            }
        }
    }

    internal static bool Opaque(ReadModelSyntax model, ConsistencyDeclarations declarations) => declarations.Slices.Any(entry =>
        (entry.Slice.Reducers ?? []).Any(reducer => ReferenceEquals(ReadModel(reducer.ReadModel, entry.Scope, declarations), model)) ||
        entry.Slice.Queries.Any(query => query.Performer is not null && ReferenceEquals(ReadModel(query.ReturnType.Name, entry.Scope, declarations), model)));
}
