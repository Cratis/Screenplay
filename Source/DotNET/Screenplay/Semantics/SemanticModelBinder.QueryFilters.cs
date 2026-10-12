// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static string? QueryFilterRefusal(QuerySyntax query, QueryParameterSyntax filter, SemanticTypeReference type, SemanticProperty? property, HashSet<string> names)
        {
            if (!query.ReturnType.IsCollection) return "requires a read-model collection.";
            if (filter.Source is not null) return "uses 'from', which is not admitted into the executable model.";
            if (!names.Add(filter.Name)) return "has a repeated name.";
            if (filter.Name == query.By?.Name) return "duplicates the 'by' argument name.";
            if (property is null) return "has no same-named read-model property.";
            if (property.Type.IsCollection || type.IsCollection || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)) return "requires a primitive, concept or enum scalar property.";
            if (type.Kind != property.Type.Kind || type.Primitive != property.Type.Primitive || type.Target != property.Type.Target) return "must have the same type as its read-model property, ignoring optionality.";

            return null;
        }

        ImmutableArray<SemanticQueryFilter> BindQueryFilters(SemanticAddress address, QuerySyntax query, BoundReadModel readModel)
        {
            var filters = ImmutableArray.CreateBuilder<SemanticQueryFilter>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var filter in query.Filters)
            {
                var type = BindTypeReference(filter.Type);
                readModel.Properties.TryGetValue(filter.Name, out var property);
                var reason = QueryFilterRefusal(query, filter, type, property, names);
                if (reason is not null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' filter '{filter.Name}' {reason}", filter.Location);
                    continue;
                }

                var id = Resolve(SemanticAddress.ForQueryArgument(address, filter.Name), filter.Location);
                filters.Add(new(id, filter.Name, type, property!.Id));
            }

            return filters.ToImmutable();
        }
    }
}
