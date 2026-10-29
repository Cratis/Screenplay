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
        readonly Dictionary<QuerySyntax, int> _performedQueryRequirements = new(ReferenceEqualityComparer.Instance);

        bool CanBindPerformedQueryArguments(QuerySyntax query)
        {
            if (query.Performer is null) return true;

            var keys = query.By is null ? query.ByParts : [query.By, .. query.ByParts];
            var parameters = keys.Concat(query.Filters).Where(parameter => parameter.Source is ContextExpressionSyntax).ToArray();
            foreach (var parameter in parameters)
            {
                _opaqueQueries.Add(query);
                var arguments = new DiagnosticArguments.Builder()
                    .Add("query", DiagnosticArgument.Text(query.Name))
                    .Add("argument", DiagnosticArgument.Text(parameter.Name)).Build();
                Error(DiagnosticCodes.ContextSourcedPerformedQueryArgument, $"Query '{query.Name}' argument '{parameter.Name}' is sourced from context, which is not supported for performer queries; use a caller-supplied argument instead.", parameter.Location, arguments);
            }
            return parameters.Length == 0;
        }

        void RequirePerformedQuery(SemanticAddress slice, QuerySyntax query)
        {
            var requirement = RequireImplementation(SemanticImplementationRole.QueryPerformer, SemanticAddress.ForQuery(slice, query.Name), query.Performer!.File, query.Performer.Code);
            if (requirement is null) return;

            // RequireImplementation appends the requirement it returns; attach the shape to that same entry.
            var shape = PerformedQueryShape(query);
            _implementationRequirements[^1] = requirement with { QueryShape = shape, ResultVersion = PerformerResultVersion(shape) };
            _performedQueryRequirements[query] = _implementationRequirements.Count - 1;
        }

        void AuthorizePerformedQuery(QuerySyntax query, string module, ImmutableArray<string> featurePath)
        {
            if (!_performedQueryRequirements.TryGetValue(query, out var index) || _implementationRequirements[index].QueryShape is not { } shape) return;

            // Policies read the query's 'by' arguments only (filters are named in the diagnostic), exactly as for a query that is bound.
            var keys = shape.Arguments.Where(argument => argument is { Kind: SemanticPerformedQueryArgumentKind.Key, Type: not null })
                .Select(argument => new SemanticProperty(default, argument.Name, argument.Type!, false));
            var authorization = EffectiveAuthorization(query.Authorize, keys, module, featurePath, queryFilterNames: query.Filters.Select(filter => filter.Name));
            _implementationRequirements[index] = _implementationRequirements[index] with { QueryShape = shape with { Authorization = authorization } };
        }

        // Decision 0119: only an observable collection performer has a live result contract (2); every other performer stays at 1.
        static uint PerformerResultVersion(SemanticPerformedQueryShape shape) => shape is { Delivery: SemanticQueryDelivery.Live, Cardinality: SemanticQueryCardinality.Many } ? 2u : 1u;

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
