// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Completeness;

static class QueryKeyCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ConsistencyDeclarations declarations)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var query in slice.Queries.Where(query => query.Performer is null))
            {
                var model = ViewBuilders.ReadModel(query.ReturnType.Name, scope, declarations);
                if (model is null || ViewBuilders.Opaque(model, declarations)) continue;
                var builders = ViewBuilders.Projections(model, declarations).ToArray();
                if (builders.Length == 0 || builders.Any(builder => builder.Projection.File is not null)) continue;

                var parts = builders.SelectMany(builder => Keys(builder.Blocks))
                    .Select(entry => entry.Key).OfType<CompositeKeySyntax>().SelectMany(key => declarations.TypeProperties(key.Type) ?? []).ToArray();
                if (query.By is { } by && !Tenant(by))
                {
                    var identity = Identity(model, declarations).ToArray();
                    if (identity.Length > 0 && identity.All(type => declarations.Compatible(by.Type, type) == false))
                    {
                        yield return Finding(query, by, model.Name);
                    }
                }

                foreach (var filter in query.Filters.Where(filter => !Tenant(filter)))
                {
                    var held = model.Properties.Concat(parts).Where(property => property.Name == filter.Name).ToArray();
                    if (held.Length == 0 || held.All(property => declarations.Compatible(filter.Type with { IsOptional = false }, property.Type) == false))
                    {
                        yield return Finding(query, filter, model.Name);
                    }
                }
            }
        }
    }

    static List<TypeRefSyntax> Identity(ReadModelSyntax model, ConsistencyDeclarations declarations)
    {
        var declared = model.Properties.Where(property => property.IsKey).ToArray();
        if (declared.Length > 0)
        {
            return [.. declared.Select(property => property.Type).Concat(declared.Length == 1 ? declarations.TypeProperties(declared[0].Type.Name)?.Select(property => property.Type) ?? [] : [])];
        }

        // The binder identifies a property by the single distinct 'by' name of queries in the owning slice.
        // The parameter's declared type is not evidence of the view's identity type.
        var owner = declarations.Slices.Single(entry => (entry.Slice.ReadModels ?? []).Any(candidate => ReferenceEquals(candidate, model)));
        var names = owner.Slice.Queries.Where(query => query.ReturnType.Name.Split('.')[^1] == model.Name && query.By is not null)
            .Select(query => query.By!.Name).Distinct(StringComparer.Ordinal).ToArray();
        if (names is [var name] && model.Properties.Where(property => property.Name == name).ToArray() is [var identity])
        {
            return [identity.Type, .. declarations.TypeProperties(identity.Type.Name)?.Select(property => property.Type) ?? []];
        }

        var types = new List<TypeRefSyntax>();
        foreach (var (_, blocks, scope) in ViewBuilders.Projections(model, declarations))
        {
            if (blocks.OfType<FromSyntax>().Any(from => from.Events.Any(source => source.Key is null && from.Key is null))) return [];
            foreach (var (key, sources) in Keys(blocks))
            {
                if (key is CompositeKeySyntax composite && declarations.TypeProperties(composite.Type) is { } parts)
                {
                    types.Add(new(composite.Type, false, false, composite.Location));
                    types.AddRange(parts.Select(property => property.Type));
                }
                else if (key is ExpressionKeySyntax { Expression: PathExpressionSyntax path })
                {
                    var resolved = sources.Select(source => declarations.Property(declarations.Event(source.Event, scope)?.Properties, path.Path, out _)?.Type).ToArray();
                    if (resolved.Length == 0 || resolved.Any(type => type is null)) return [];
                    types.AddRange(resolved.OfType<TypeRefSyntax>());
                }
                else
                {
                    // Literal, context and opaque key expressions do not establish a nominal identity type.
                    return [];
                }
            }
        }

        return types;
    }

    static IEnumerable<(KeySyntax Key, IEnumerable<EventSpecSyntax> Sources)> Keys(IReadOnlyList<ProjectionBlockSyntax> blocks)
    {
        foreach (var from in blocks.OfType<FromSyntax>())
        {
            foreach (var source in from.Events)
            {
                var key = source.Key is { } expression ? new ExpressionKeySyntax(expression, source.Location) : from.Key;
                if (key is not null) yield return (key, [source]);
            }
        }
    }

    static bool Tenant(QueryParameterSyntax parameter) => parameter.Source is ContextExpressionSyntax context && context.Path == "tenant";

    static Diagnostic Finding(QuerySyntax query, QueryParameterSyntax parameter, string model) => Diagnostic.Warning(
        DiagnosticCodes.QueryParameterNotHeldByView,
        $"Query '{query.Name}' parameter '{parameter.Name}' of type '{parameter.Type.Name}' is not held by read model '{model}' identity or fields",
        parameter.Location);
}
