// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticSpecification? BindSpecification(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands)
        {
            if (specification.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' file reference is realization provenance.", specification.File.Location);
            }

            SemanticCommand? command = null;
            if (specification.When is not null && !commands.TryGetValue(ShortName(specification.When.CommandType), out command))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification '{specification.Name}' command is unresolved in its slice.", specification.When.Location);
                return null;
            }

            if (specification.When?.For is not null)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Specification command destination assertion 'for' on '{specification.When.CommandType}' is reserved for ESM v2 (issue #226).",
                    specification.When.For.Location);
            }

            if (specification.When is null && (specification.ThenEvents.Any() || specification.ThenErrors.Any() ||
                (!(specification.ThenReadModels?.Any() ?? false) && !specification.ThenQueries.Any())))
            {
                Error(DiagnosticCodes.InvalidWhenlessSpecification, "A specification without 'when' requires at least one 'then readmodel' or 'then query' and cannot assert 'then' events or errors.", specification.Location);
            }

            var address = SemanticAddress.ForSpecification(slice, specification.Name);
            var id = Resolve(address, specification.Location);
            var givenEvents = specification.Given.Select(BindSpecificationEvent).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var givenReadModels = (specification.GivenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var when = specification.When is null ? null : new SemanticSpecificationCommand(
                command!.Id,
                BindPropertyValues(specification.When.Values, command.Properties.ToDictionary(_ => _.Name, StringComparer.Ordinal), "specification command"));
            var thenEvents = specification.ThenEvents.Select(BindSpecificationEvent).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenReadModels = (specification.ThenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var thenQueries = specification.ThenQueries.Select(BindSpecificationQuery).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenErrors = specification.ThenErrors.Select(value => new SemanticSpecificationError(null, value.Name)).ToImmutableArray();
            return new(
                id,
                specification.Name,
                givenEvents,
                givenReadModels,
                when,
                thenEvents,
                thenReadModels,
                thenQueries,
                thenErrors);
        }

        SemanticSpecificationEvent? BindSpecificationEvent(SpecificationEventSyntax value)
        {
            if (value.For is not null)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Specification event-source assertion 'for' on '{value.EventType}' is reserved for ESM v2 (issue #226).",
                    value.For.Location);
            }

            if (!_events.TryGetValue(ShortName(value.EventType), out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification event '{value.EventType}' is unresolved.", value.Location);
                return null;
            }

            return new(
                @event.Contract.Id,
                BindPropertyValues(value.Values, @event.Properties, "specification event"));
        }

        SemanticSpecificationReadModel? BindReadModelState(
            string name,
            IEnumerable<PropertyMappingSyntax> values,
            SourceLocation location)
        {
            if (!_readModels.TryGetValue(ShortName(name), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification read model '{name}' is unresolved.", location);
                return null;
            }

            return BindReadModelState(readModel, values, location, null);
        }

        SemanticSpecificationReadModel? BindReadModelState(
            BoundReadModel readModel,
            IEnumerable<PropertyMappingSyntax> values,
            SourceLocation location,
            SemanticValue? inferredKey)
        {
            var bound = BindPropertyValues(values, readModel.Properties, "specification read model");
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);
            var key = identifier is null ? null : bound.SingleOrDefault(_ => _.TargetProperty == identifier.Id)?.Value ?? inferredKey;
            if (key is null)
            {
                Error(DiagnosticCodes.MissingSpecificationReadModelIdentifier, $"Specification read model '{readModel.Model.Name}' must state its identifier property '{identifier?.Name}' in this block.", location);
                return null;
            }

            return new(readModel.Model.Id, key, bound);
        }

        SemanticSpecificationQueryResult? BindSpecificationQuery(SpecificationQuerySyntax value)
        {
            if (!_queries.TryGetValue(ShortName(value.Query), out var query))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification query '{value.Query}' is unresolved.", value.Location);
                return null;
            }

            var arguments = value.Arguments.ToArray();
            if (arguments.Length != 1 || arguments[0].Property != query.Argument.Name || BindConcreteValue(arguments[0].Source, "specification query argument") is not { } key)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification query '{value.Query}' must state exactly its keyed argument in Program v1.", value.Location);
                return null;
            }

            var readModel = _readModels.Values.Single(_ => _.Model.Id == query.ReadModel);
            var results = value.Results
                .Select(result => BindReadModelState(readModel, result.Properties, result.Location, key))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(query.Id, key, results);
        }

        ImmutableArray<SemanticPropertyValue> BindPropertyValues(
            IEnumerable<PropertyMappingSyntax> values,
            Dictionary<string, SemanticProperty> properties,
            string description)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            foreach (var value in values)
            {
                if (!properties.TryGetValue(value.Property, out var property))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"The {description} property '{value.Property}' is unresolved.", value.Location);
                    continue;
                }

                if (value.Source is LiteralExpressionSyntax { Value: null } &&
                    (description == "specification command" || description == "specification event"))
                {
                    Error(DiagnosticCodes.NullSpecificationFact, $"A {description} cannot contain null: in Chronicle, an optional fact is a separate event.", value.Source.Location);
                    continue;
                }

                if (BindConcreteValue(value.Source, description) is { } concrete)
                {
                    bound.Add(new(property.Id, concrete));
                }
            }

            return bound.ToImmutable();
        }

        SemanticValue? BindConcreteValue(ExpressionSyntax expression, string description)
        {
            if (expression is LiteralExpressionSyntax literal)
            {
                return BindLiteral(literal);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} requires a concrete portable value in Program v1.", expression.Location);
            return null;
        }
    }
}
