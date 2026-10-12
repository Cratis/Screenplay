// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesQueryFilters(SemanticApplication application) => application.Modules.Any(module => module.Features.Any(UsesQueryFilters));

    static bool UsesQueryFilters(SemanticFeature feature) => feature.Slices.Any(slice => slice.Queries.Any(query => !query.Filters.IsDefaultOrEmpty)) || feature.Features.Any(UsesQueryFilters);

    static void ValidateQueryFilterVersion(SemanticApplication application, SemanticVersion version)
    {
        var uses = UsesQueryFilters(application);
        if (uses && !version.IsAtLeast(SemanticVersion.V14)) throw new InvalidSemanticContract("Query filters require ESM v14 or later.");
        if (!uses && version == SemanticVersion.V14) throw new InvalidSemanticContract("An ESM v14 model must use query filters.");
    }

    private sealed partial class ValidationContext
    {
        void ValidateQueryFilters(SemanticKeyedQuery query, SemanticReadModel readModel)
        {
            RequireObjects(query.Filters, nameof(query.Filters), "query filter");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var filter in query.Filters)
            {
                ValidateTypeReference(filter.Type);
                var property = readModel.Properties.SingleOrDefault(property => property.Id == filter.Property);
                if (query.Cardinality != SemanticQueryCardinality.Many) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' requires a collection.");
                if (!names.Add(filter.Name) || filter.Name == query.Argument?.Name) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' has a duplicate argument name.");
                if (property is null || property.Name != filter.Name) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' requires a same-named property.");
                if (filter.Type.IsCollection || property.Type.IsCollection || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' requires a primitive, concept or enum scalar property.");
                if (!SameValueType(filter.Type, property.Type)) throw new InvalidSemanticContract($"Query '{query.Name}' filter '{filter.Name}' must have the property type, ignoring optionality.");
            }
        }
    }
}
