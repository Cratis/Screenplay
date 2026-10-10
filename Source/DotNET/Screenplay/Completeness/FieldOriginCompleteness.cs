// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Completeness;

static class FieldOriginCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ConsistencyDeclarations declarations)
    {
        foreach (var model in declarations.Slices.SelectMany(entry => entry.Slice.ReadModels ?? []).Where(model => !ViewBuilders.Opaque(model, declarations)))
        {
            var builders = ViewBuilders.Projections(model, declarations).ToArray();
            if (builders.Length == 0)
            {
                yield return Diagnostic.Warning(
                    DiagnosticCodes.ReadModelFieldWithoutOrigin,
                    $"Read model '{model.Name}' has no projection, reducer or performer that builds or serves it",
                    model.Location);
                continue;
            }

            foreach (var (projection, blocks, scope) in builders.Where(builder => builder.Projection.File is null))
            {
                var properties = model.Properties.ToArray();
                var covered = ProjectionFieldCoverage.Covered(blocks, properties, ProjectionFieldCoverage.Enabled(projection.AutoMap, true), [], scope, declarations, false, true);
                if (covered is null)
                {
                    continue;
                }

                var owner = declarations.Slices.Single(entry => (entry.Slice.ReadModels ?? []).Any(candidate => ReferenceEquals(candidate, model)));
                var identities = owner.Slice.Queries.Where(query => query.ReturnType.Name.Split('.')[^1] == model.Name && query.By is not null)
                    .Select(query => query.By!.Name).Distinct(StringComparer.Ordinal).ToArray();
                var declared = properties.Where(property => property.IsKey).ToArray();
                if (declared.Length > 0) covered.UnionWith(declared.Select(property => property.Name));
                else if (identities is [var identity] && properties.Any(property => property.Name == identity)) covered.Add(identity);

                // Composite keys declare target parts, unlike a scalar key expression which names its source.
                covered.UnionWith(blocks.OfType<FromSyntax>().Select(from => from.Key).OfType<CompositeKeySyntax>()
                    .SelectMany(key => key.Parts).Select(part => ProjectionFieldCoverage.Root(part.Property)));
                foreach (var missing in properties.Where(property => !covered.Contains(property.Name)))
                {
                    yield return Diagnostic.Warning(
                        DiagnosticCodes.ReadModelFieldWithoutOrigin,
                        $"Read model '{model.Name}' field '{missing.Name}' has no declared projection origin",
                        missing.Location);
                }
            }
        }
    }
}
