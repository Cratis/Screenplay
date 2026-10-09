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
        static SemanticQueryCardinality QueryCardinality(TypeRefSyntax returnType) => returnType switch
        {
            { IsCollection: true } => SemanticQueryCardinality.Many,
            { IsOptional: true } => SemanticQueryCardinality.ZeroOrOne,
            _ => SemanticQueryCardinality.One
        };

        BoundReadModel BindReadModel(SemanticAddress slice, SliceSyntax owner, ReadModelSyntax readModel)
        {
            if (readModel.Documentation is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' documentation is authoring metadata.", readModel.Location);
            }

            if (readModel.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' description is authoring metadata.", readModel.Location);
            }

            if (readModel.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' file reference is realization provenance.", readModel.File.Location);
            }

            var identifierNames = owner.Queries
                .Where(query => ShortName(query.ReturnType.Name) == readModel.Name && query.By is not null && !query.ReturnType.IsCollection)
                .Select(query => query.By!.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var collectionKeyNames = owner.Queries
                .Where(query => ShortName(query.ReturnType.Name) == readModel.Name && query.By is not null && query.ReturnType.IsCollection)
                .Select(query => query.By!.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var hasAdmittedListQuery = owner.Queries.Any(query =>
                ShortName(query.ReturnType.Name) == readModel.Name &&
                query.ReturnType.IsCollection &&
                query.By?.Source is null &&
                !query.Filters.Any() &&
                query.Scope is null &&
                query.Performer is null);
            if (identifierNames.Length == 0 && hasAdmittedListQuery)
            {
                var conventionalIdentifiers = readModel.Properties
                    .Select(property => property.Name)
                    .Where(name => name.EndsWith("Id", StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                var identifiersOutsideCollectionKeys = conventionalIdentifiers
                    .Except(collectionKeyNames, StringComparer.Ordinal)
                    .ToArray();
                identifierNames = identifiersOutsideCollectionKeys.Length == 1 ? identifiersOutsideCollectionKeys : conventionalIdentifiers;
            }

            if (identifierNames.Length != 1)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Read model '{readModel.Name}' must have one unambiguous keyed query or one conventional '*Id' property to identify instances in the admitted ESM query shapes.",
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

            if (query.Filters.Any() || query.Scope is not null || query.Performer is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' uses filtering, scope, or implementation behavior outside the admitted ESM query shapes.", query.Location);
            }

            if (query.By?.Source is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' must use caller-supplied 'by' arguments in the admitted ESM query shapes.", query.By.Location);
                return null;
            }

            var cardinality = QueryCardinality(query.ReturnType);
            if (cardinality == SemanticQueryCardinality.One)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' must return an optional read model or a read-model collection in the admitted ESM query shapes.", query.ReturnType.Location);
                return null;
            }

            if (query.By is null && cardinality != SemanticQueryCardinality.Many)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' without a 'by' argument must return a read-model collection in the admitted ESM query shapes.", query.Location);
                return null;
            }

            var readModelName = ShortName(query.ReturnType.Name);
            if (!_readModels.TryGetValue(readModelName, out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Query '{query.Name}' read model is unresolved.", query.Location);
                return null;
            }

            var address = SemanticAddress.ForQuery(slice, query.Name);
            var id = Resolve(address, query.Location);
            SemanticReadModelQueryArgument? argument = null;
            SemanticId? keyPropertyId = null;
            if (query.By is not null)
            {
                if (!readModel.Properties.TryGetValue(query.By.Name, out var keyProperty))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Query '{query.Name}' key property is unresolved.", query.By.Location);
                    return null;
                }

                var argumentAddress = SemanticAddress.ForQueryArgument(address, query.By.Name);
                var argumentId = Resolve(argumentAddress, query.By.Location);
                argument = new SemanticReadModelQueryArgument(argumentId, query.By.Name, BindTypeReference(query.By.Type));
                keyPropertyId = keyProperty.Id;
            }

            return new(
                id,
                query.Name,
                argument,
                readModel.Model.Id,
                keyPropertyId,
                cardinality,
                query.IsObservable ? SemanticQueryDelivery.Live : SemanticQueryDelivery.Snapshot);
        }
    }
}
