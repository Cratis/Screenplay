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
            if (readModel.IsPassive)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' passive is realization metadata.", readModel.Location);
            }

            if (readModel.Sink is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Read model '{readModel.Name}' sink '{readModel.Sink}' is realization metadata.", readModel.DirectiveLocations.GetValueOrDefault("sink", readModel.Location));
            }

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

            var explicitKeys = readModel.Properties.Where(property => property.IsKey).Select(property => property.Name).ToArray();
            var identifierNames = ReadModelIdentity.IdentifierNames(owner, readModel);

            if (identifierNames.Length != 1 && explicitKeys.Length < 2)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Read model '{readModel.Name}' must have one unambiguous keyed query or one conventional '*Id' property to identify instances in the admitted ESM query shapes.",
                    readModel.Location);
            }

            var address = SemanticAddress.ForReadModel(slice, readModel.Name);
            var id = Resolve(address, readModel.Location);
            var properties = readModel.Properties
                .Select(property => BindProperty(address, property, identifierNames.Contains(property.Name, StringComparer.Ordinal)))
                .ToImmutableArray();
            return new(
                readModel,
                new(id, readModel.Name, properties),
                properties.ToDictionary(_ => _.Name, StringComparer.Ordinal))
            {
                Address = address
            };
        }

        SemanticKeyedQuery? BindQuery(SemanticAddress slice, QuerySyntax query)
        {
            if (query.ByParts.Any() && (query.ReturnType.IsCollection || !query.ReturnType.IsOptional || query.ByParts.Any(part => part.Source is not null)))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' by block requires a caller-supplied complete key and an optional single read model; use equality filters for collections.", query.Location);
                return null;
            }

            if (query.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Query '{query.Name}' description is authoring metadata.", query.Location);
            }

            if (query.Performer is not null)
            {
                // A query a performer answers is an opaque read: the code decides what it returns, so no declarative
                // shape can be executed. It stays listed as code the model needs and is excluded from the executable
                // model and from specifications, instead of refusing every other declaration of the application.
                RequirePerformedQuery(slice, query);
                _opaqueQueries.Add(query);
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Query '{query.Name}' is performed by code: it is an opaque read, so it is realization metadata and no specification can execute it.", query.Location);
                return null;
            }

            if (query.Scope is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' uses 'scoped to', which is not admitted into the executable model.", query.Location);
            }

            if (!query.ReturnType.IsCollection && query.Filters.Any())
            {
                foreach (var filter in query.Filters)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' filter '{filter.Name}' requires a read-model collection.", filter.Location);
                }
                return null;
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

            if (query.By is null && !query.ByParts.Any() && cardinality != SemanticQueryCardinality.Many)
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

            var narrowing = BindNarrowing(query, cardinality, readModel);
            return new(
                id,
                query.Name,
                argument,
                readModel.Model.Id,
                keyPropertyId,
                cardinality,
                query.IsObservable ? SemanticQueryDelivery.Live : SemanticQueryDelivery.Snapshot)
            {
                Filters = BindQueryFilters(address, query, readModel),
                KeyParts = BindQueryKeyParts(address, query, readModel),
                Narrowing = narrowing
            };
        }

        SemanticPolicyReference? BindNarrowing(QuerySyntax query, SemanticQueryCardinality cardinality, BoundReadModel readModel)
        {
            if (query.Narrow is not { } narrow) return null;
            var policy = syntax.Policies.FirstOrDefault(candidate => candidate.Name == narrow.Policy);
            if (policy is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Query '{query.Name}' narrows by policy '{narrow.Policy}', which is unresolved.", narrow.Location);
                return null;
            }

            if (cardinality != SemanticQueryCardinality.Many)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' narrows rows, which needs a read-model collection result.", narrow.Location);
                return null;
            }

            if (policy.Condition is not StateConditionSyntax and not LogicalPolicyConditionSyntax and not NotPolicyConditionSyntax and not AuthenticatedConditionSyntax and not RoleConditionSyntax and not ClaimConditionSyntax)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' has an opaque predicate and cannot narrow the rows of query '{query.Name}'.", narrow.Location);
                return null;
            }

            foreach (var read in policy.Reads ?? [])
            {
                var mapped = (policy.Inputs ?? []).Any(input => input.Name == read.By) ? null : read.By;
                if (mapped is null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' read '{read.ReadModel}' takes its key from input '{read.By}'; to narrow rows, key the read by a property of '{ShortName(query.ReturnType.Name)}'.", narrow.Location);
                    return null;
                }

                if (!readModel.Properties.ContainsKey(mapped))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' read '{read.ReadModel}' takes its key from '{mapped}', which is not a property of '{ShortName(query.ReturnType.Name)}'.", narrow.Location);
                    return null;
                }
            }

            return new SemanticPolicyReference(policy.Name);
        }
    }
}
