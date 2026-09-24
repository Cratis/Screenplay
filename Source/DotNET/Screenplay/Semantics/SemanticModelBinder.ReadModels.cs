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
        BoundReadModel BindReadModel(SemanticAddress slice, SliceSyntax owner, ReadModelSyntax readModel)
        {
            if (readModel.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' description is authoring metadata.", readModel.Location);
            }

            if (readModel.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' file reference is realization provenance.", readModel.File.Location);
            }

            var identifierNames = owner.Queries
                .Where(query => ShortName(query.ReturnType.Name) == readModel.Name && query.By is not null)
                .Select(query => query.By!.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (identifierNames.Length != 1)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Read model '{readModel.Name}' must have one unambiguous keyed query to identify instances in the first ESM v1 vertical.",
                    readModel.Location);
            }

            var identifier = identifierNames.Length == 1 ? identifierNames[0] : null;
            var address = SemanticAddress.ForReadModel(slice, readModel.Name);
            var id = Resolve(address, readModel.Location);
            var properties = readModel.Properties
                .Select(property => BindProperty(address, property, property.Name == identifier))
                .ToImmutableArray();
            return new(
                readModel,
                new(id, readModel.Name, properties),
                properties.ToDictionary(_ => _.Name, StringComparer.Ordinal));
        }

        SemanticKeyedQuery? BindQuery(SemanticAddress slice, QuerySyntax query)
        {
            if (query.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Query '{query.Name}' description is authoring metadata.", query.Location);
            }

            if (query.Performer is not null)
            {
                RequireImplementation(SemanticImplementationRole.QueryPerformer, SemanticAddress.ForQuery(slice, query.Name), query.Performer.File, query.Performer.Code);
            }

            if (query.IsObservable || query.Filters.Any() || query.Scope is not null || query.Performer is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' uses delivery, filtering, scope, or implementation behavior outside the first ESM v1 vertical.", query.Location);
            }

            if (query.By is null || query.By.Source is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' must declare one caller-supplied 'by' argument in the first ESM v1 vertical.", query.Location);
                return null;
            }

            if (query.ReturnType.IsCollection || !query.ReturnType.IsOptional)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' must return one optional read model in the first ESM v1 vertical.", query.ReturnType.Location);
            }

            var readModelName = ShortName(query.ReturnType.Name);
            if (!_readModels.TryGetValue(readModelName, out var readModel) || !readModel.Properties.TryGetValue(query.By.Name, out var keyProperty))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Query '{query.Name}' read model or key property is unresolved.", query.Location);
                return null;
            }

            var address = SemanticAddress.ForQuery(slice, query.Name);
            var id = Resolve(address, query.Location);
            var argumentAddress = SemanticAddress.ForQueryArgument(address, query.By.Name);
            var argumentId = Resolve(argumentAddress, query.By.Location);
            var argument = new SemanticReadModelQueryArgument(argumentId, query.By.Name, BindTypeReference(query.By.Type));
            return new(
                id,
                query.Name,
                argument,
                readModel.Model.Id,
                keyProperty.Id,
                SemanticQueryCardinality.ZeroOrOne,
                SemanticQueryDelivery.Snapshot);
        }
    }
}
