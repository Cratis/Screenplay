// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        void RequirePerformedQuery(SemanticAddress slice, QuerySyntax query)
        {
            var requirement = RequireImplementation(SemanticImplementationRole.QueryPerformer, SemanticAddress.ForQuery(slice, query.Name), query.Performer!.File, query.Performer.Code);
            if (requirement is null) return;

            // RequireImplementation appends the requirement it returns; attach the shape to that same entry.
            _implementationRequirements[^1] = requirement with { QueryShape = PerformedQueryShape(query) };
        }

        SemanticPerformedQueryShape PerformedQueryShape(QuerySyntax query)
        {
            var readModelName = ShortName(query.ReturnType.Name);
            var readModel = _readModels.GetValueOrDefault(readModelName);
            return new(
                readModelName,
                readModel?.Model.Id,
                readModel?.Address,
                QueryCardinality(query.ReturnType),
                query.IsObservable ? SemanticQueryDelivery.Live : SemanticQueryDelivery.Snapshot,
                PerformedQueryArguments(query));
        }

        ImmutableArray<SemanticPerformedQueryArgument> PerformedQueryArguments(QuerySyntax query)
        {
            // Only caller-supplied values are arguments: a parameter sourced with 'from' is filled by the host.
            var keys = (query.By is null ? query.ByParts : [query.By, .. query.ByParts])
                .Select(parameter => (Parameter: parameter, Kind: SemanticPerformedQueryArgumentKind.Key));
            var filters = query.Filters.Select(parameter => (Parameter: parameter, Kind: SemanticPerformedQueryArgumentKind.Filter));
            return [.. keys.Concat(filters)
                .Where(entry => entry.Parameter.Source is null)
                .Select(entry => new SemanticPerformedQueryArgument(
                    entry.Parameter.Name,
                    entry.Kind,
                    TryBindTypeReference(entry.Parameter.Type),
                    entry.Parameter.Type.IsOptional))];
        }

        SemanticTypeReference? TryBindTypeReference(TypeRefSyntax type)
        {
            // A performed query is authoring-only, so an unresolved argument type is described, never a binding failure.
            try
            {
                return BindTypeReference(type);
            }
            catch (InvalidSemanticContract)
            {
                return null;
            }
        }
    }
}
