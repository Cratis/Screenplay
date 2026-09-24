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

            var deniedQuery = specification.ThenDenied is not null && specification.When is null && specification.WhenAppended is null &&
                specification.ThenQueries.Count() == 1 && !specification.ThenQueries.Single().Results.Any();
            if (specification.When is null && specification.WhenAppended is null && (specification.ThenEvents.Any() || specification.ThenErrors.Any() ||
                (specification.ThenDenied is not null && !deniedQuery) ||
                (!(specification.ThenReadModels?.Any() ?? false) && !specification.ThenQueries.Any())))
            {
                Error(DiagnosticCodes.InvalidWhenlessSpecification, "A specification without 'when' requires 'then readmodel' or 'then query'; denial requires one query with arguments and no results.", specification.Location);
            }

            if (specification.ThenDenied is not null && (specification.ThenErrors.Any() || specification.ThenEvents.Any() ||
                (specification.ThenReadModels?.Any() ?? false) || (specification.ThenQueries.Any() && !deniedQuery)))
            {
                Error(DiagnosticCodes.InvalidSpecificationDenied, "'then denied' cannot be combined with success or error outcomes.", specification.ThenDenied.Location);
            }

            if ((command?.Authorization is not null || specification.ThenQueries.Any(query =>
                _queries.TryGetValue(ShortName(query.Query), out var referenced) && referenced.Authorization is not null)) && specification.GivenCaller is null)
            {
                Error(DiagnosticCodes.MissingSpecificationCaller, "An authorized specification requires an explicit 'given caller' fixture; missing identity context is never assumed.", specification.Location);
            }

            var address = SemanticAddress.ForSpecification(slice, specification.Name);
            var id = Resolve(address, specification.Location);
            var givenEvents = specification.Given.Select(value => BindSpecificationEvent(value, commands)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var givenReadModels = (specification.GivenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location, value.Exactly))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var when = specification.When is null ? null : new SemanticSpecificationCommand(
                command!.Id,
                BindPropertyValues(specification.When.Values, command.Properties.ToDictionary(_ => _.Name, StringComparer.Ordinal), "specification command"))
            {
                EventSource = specification.When.For is null ? null : BindEventSource(specification.When.For, command.Destination?.Type ?? command.Properties.SingleOrDefault(_ => _.IsIdentifier)?.Type)
            };
            var thenEvents = specification.ThenEvents.Select(value => BindSpecificationEvent(value, commands)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenReadModels = (specification.ThenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location, value.Exactly))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var thenQueries = specification.ThenQueries.Select(BindSpecificationQuery).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenErrors = specification.ThenErrors.Select(value =>
            {
                ValidateStringKey(value.Name, value.Location);
                return new SemanticSpecificationError(null, value.Name);
            }).ToImmutableArray();
            return new(
                id,
                specification.Name,
                givenEvents,
                givenReadModels,
                when,
                thenEvents,
                thenReadModels,
                thenQueries,
                thenErrors)
            {
                GivenCaller = specification.GivenCaller is null ? null : new(
                    specification.GivenCaller.Authenticated,
                    [.. specification.GivenCaller.Roles],
                    [.. specification.GivenCaller.Claims.Select(claim => new SemanticCallerClaim(claim.Type, claim.Value))]),
                ThenDenied = specification.ThenDenied is not null,
                WhenAppended = specification.WhenAppended is null ? null : BindSpecificationAppend(specification.WhenAppended, commands),
                ThenEventsInAnyOrder = specification.ThenEventsInAnyOrder
            };
        }

        SemanticSpecificationAppend? BindSpecificationAppend(SpecificationEventSyntax value, Dictionary<string, SemanticCommand> commands)
        {
            var bound = BindSpecificationEvent(value, commands);
            return bound is null ? null : new(bound.EventContract, bound.Values) { EventSource = bound.EventSource };
        }

        SemanticSpecificationEvent? BindSpecificationEvent(SpecificationEventSyntax value, Dictionary<string, SemanticCommand> commands)
        {
            if (!_events.TryGetValue(ShortName(value.EventType), out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification event '{value.EventType}' is unresolved.", value.Location);
                return null;
            }

            var types = commands.Values.SelectMany(command => command.Produces
                .Where(produced => produced.EventContract == @event.Contract.Id)
                .Select(_ => command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type))
                .OfType<SemanticTypeReference>().Distinct().ToArray();
            var type = types.Length == 1 ? types[0] : null;
            return new(
                @event.Contract.Id,
                BindPropertyValues(value.Values, @event.Properties, "specification event"))
            {
                EventSource = value.For is null ? null : BindEventSource(value.For, type)
            };
        }

        SemanticEventSourceIdentity? BindEventSource(ExpressionSyntax expression, SemanticTypeReference? type)
        {
            if (type is null or { IsCollection: true } or { IsOptional: true })
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "An event-source assertion needs one unambiguous required scalar command destination type.", expression.Location);
                return null;
            }

            if (BindConcreteValue(expression, type, "specification event source", false) is not { } value)
            {
                return null;
            }

            UsesV2 = true;
            return new(type, value);
        }

        SemanticSpecificationReadModel? BindReadModelState(
            string name,
            IEnumerable<PropertyMappingSyntax> values,
            SourceLocation location,
            bool exactly)
        {
            if (!_readModels.TryGetValue(ShortName(name), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification read model '{name}' is unresolved.", location);
                return null;
            }

            return BindReadModelState(readModel, values, location, null) is { } state ? state with { Exactly = exactly } : null;
        }

        SemanticSpecificationReadModel? BindReadModelState(
            BoundReadModel readModel,
            IEnumerable<PropertyMappingSyntax> values,
            SourceLocation location,
            SemanticValue? inferredKey)
        {
            var bound = BindPropertyValues(values, readModel.Properties, "specification read model");
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);

            // A read model without an identifier has already been reported where it, or its keyed query, is declared.
            // Asking every block that uses it for an identifier nobody can name would only repeat that error.
            if (identifier is null)
            {
                return null;
            }

            var key = bound.SingleOrDefault(_ => _.TargetProperty == identifier.Id)?.Value ?? inferredKey;
            if (key is null)
            {
                Error(DiagnosticCodes.MissingSpecificationReadModelIdentifier, $"Specification read model '{readModel.Model.Name}' must state its identifier property '{identifier.Name}' in this block.", location);
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
            if (arguments.Length != 1 || arguments[0].Property != query.Argument.Name || BindConcreteValue(arguments[0].Source, query.Argument.Type, "specification query argument", false) is not { } key)
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
            return new(query.Id, key, results) { Exactly = value.Exactly };
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

                if (BindConcreteValue(value.Source, property.Type, description, description == "specification read model") is { } concrete)
                {
                    bound.Add(new(property.Id, concrete));
                }
            }

            return bound.ToImmutable();
        }

        SemanticValue? BindConcreteValue(ExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull)
        {
            if (expression is LiteralExpressionSyntax { Value: null })
            {
                if (!allowNull && (description == "specification command" || description == "specification event"))
                {
                    Error(DiagnosticCodes.NullSpecificationFact, $"A {description} cannot contain null: in Chronicle, an optional fact is a separate event.", expression.Location);
                    return null;
                }

                if (!allowNull || !target.IsOptional)
                {
                    Error(DiagnosticCodes.InvalidSpecificationNull, $"A null {description} requires an optional read-model property.", expression.Location);
                    return null;
                }

                return SemanticValue.Null;
            }

            if (expression is ListExpressionSyntax list)
            {
                if (!target.IsCollection)
                {
                    return InvalidShape(expression, description, "a scalar or object");
                }

                var values = ImmutableArray.CreateBuilder<SemanticValue>();
                var valid = true;
                foreach (var item in list.Items)
                {
                    var bound = BindConcreteValue(item, target with { IsCollection = false, IsOptional = false }, description, allowNull);
                    if (bound is null)
                    {
                        valid = false;
                    }
                    else
                    {
                        values.Add(bound);
                    }
                }

                return valid ? SemanticValue.Array(values.ToImmutable()) : null;
            }

            if (expression is ObjectExpressionSyntax obj)
            {
                if (target.IsCollection || target.Kind != SemanticTypeReferenceKind.CompositeType)
                {
                    return InvalidShape(expression, description, target.IsCollection ? "a list" : "a scalar");
                }

                return BindStructuredValue(obj, target, description, allowNull);
            }

            if (target.IsCollection || target.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                return InvalidShape(expression, description, target.IsCollection ? "a list" : "an object");
            }

            if (expression is LiteralExpressionSyntax literal)
            {
                return BindLiteral(literal);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} requires a concrete portable value in Program v1.", expression.Location);
            return null;
        }

        SemanticValue? BindStructuredValue(ObjectExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull)
        {
            var declaration = (syntax.Types ?? []).SingleOrDefault(type =>
                _types.TryGetValue(type.Name, out var registered) && registered.Id == target.Target);
            if (declaration is null)
            {
                Error(DiagnosticCodes.UnresolvedStructuredValueType, $"The {description} composite type cannot be resolved.", expression.Location);
                return null;
            }

            var declared = declaration.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            var properties = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var valid = true;
            foreach (var member in expression.Members)
            {
                if (!assigned.Add(member.Name))
                {
                    Error(DiagnosticCodes.DuplicateSemanticValueMember, $"Duplicate property '{member.Name}' in {description}.", member.Location);
                    valid = false;
                    continue;
                }

                if (!declared.TryGetValue(member.Name, out var property))
                {
                    Error(DiagnosticCodes.UnknownStructuredValueMember, $"Unknown property '{member.Name}' in structured value for '{declaration.Name}'.", member.Location);
                    valid = false;
                    continue;
                }

                var value = BindConcreteValue(member.Value, BindTypeReference(property.Type), description, allowNull);
                if (value is null)
                {
                    valid = false;
                    continue;
                }

                var address = SemanticAddress.ForProperty(_types[declaration.Name].Address, property.Name);
                var id = documents.IdentityCatalog.ResolveSemanticAssignment(address).Id;
                properties.Add(new(id, value));
            }

            foreach (var property in declaration.Properties.Where(property => !property.Type.IsOptional && !assigned.Contains(property.Name)))
            {
                Error(DiagnosticCodes.MissingStructuredValueMember, $"The {description} composite '{declaration.Name}' requires property '{property.Name}'.", expression.Location);
                valid = false;
            }

            return valid ? SemanticValue.Composite(properties.ToImmutable()) : null;
        }

        SemanticValue? InvalidShape(ExpressionSyntax expression, string description, string expected)
        {
            Error(DiagnosticCodes.IncompatibleStructuredValue, $"The {description} value must be {expected} for its declared type.", expression.Location);
            return null;
        }
    }
}
