// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticQueryFilterRules
{
    internal static void ValidateValues(SemanticKeyedQuery query, ImmutableArray<SemanticQueryFilterValue> values, Action<SemanticValue, SemanticTypeReference, string> validate)
    {
        if (values.IsDefault) throw new InvalidSemanticContract($"Query '{query.Name}' filter values must be initialized.");
        var assigned = new HashSet<SemanticId>();
        foreach (var value in values)
        {
            if (value is null) throw new InvalidSemanticContract($"Query '{query.Name}' filter value is missing.");
            var filter = query.Filters.SingleOrDefault(filter => filter.Id == value.Filter) ??
                throw new InvalidSemanticContract($"Query '{query.Name}' filter '{value.Filter}' is unknown.");
            if (!assigned.Add(value.Filter)) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' is repeated.");
            if (value.Value is null or SemanticNullValue) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' cannot be null.");
            validate(value.Value, filter.Type, $"query '{query.Name}' filter '{filter.Name}'");
        }

        foreach (var filter in query.Filters.Where(filter => !filter.Type.IsOptional && !assigned.Contains(filter.Id)))
        {
            throw new InvalidSemanticContract($"Query '{query.Name}' required filter '{filter.Name}' is missing.");
        }
    }

    internal static bool SameValues(ImmutableArray<SemanticQueryFilterValue> left, ImmutableArray<SemanticQueryFilterValue> right) =>
        left.Length == right.Length && left.All(value => right.Any(candidate => candidate.Filter == value.Filter && SemanticValueRules.AreEqual(candidate.Value, value.Value)));
}
