// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

internal static class ReadModelIdentity
{
    internal static string[] IdentifierNames(SliceSyntax owner, ReadModelSyntax readModel)
    {
        var explicitKeys = readModel.Properties.Where(property => property.IsKey).Select(property => property.Name).ToArray();
        if (explicitKeys.Length > 0) return explicitKeys;

        var queries = owner.Queries.Where(query => query.ReturnType.Name.Split('.')[^1] == readModel.Name).ToArray();
        var identifiers = queries.Where(query => query.By is not null && !query.ReturnType.IsCollection)
            .Select(query => query.By!.Name).Distinct(StringComparer.Ordinal).ToArray();
        if (identifiers.Length > 0) return identifiers;
        return queries.Any(IsAdmittedListQuery) ? ConventionalIdentifiers(readModel, queries) : [];
    }

    static bool IsAdmittedListQuery(QuerySyntax query) => query.ReturnType.IsCollection && query.By?.Source is null &&
        query.Filters.All(filter => filter.Source is null) && query.Scope is null;

    static string[] ConventionalIdentifiers(ReadModelSyntax readModel, IEnumerable<QuerySyntax> queries)
    {
        var collectionKeys = queries.Where(query => query.By is not null && query.ReturnType.IsCollection)
            .Select(query => query.By!.Name).Distinct(StringComparer.Ordinal);
        var conventional = readModel.Properties.Select(property => property.Name)
            .Where(name => name.EndsWith("Id", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToArray();
        var outsideCollectionKeys = conventional.Except(collectionKeys, StringComparer.Ordinal).ToArray();
        return outsideCollectionKeys.Length == 1 ? outsideCollectionKeys : conventional;
    }
}
