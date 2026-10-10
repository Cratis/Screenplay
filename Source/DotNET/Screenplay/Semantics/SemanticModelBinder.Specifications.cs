// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.None, 1000)]
        private static partial Regex IsoInstant();

        static IEnumerable<SliceSyntax> AllSlices(FeatureSyntax feature) =>
            feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

        static bool CommandBelongsToSlice(string reference, SemanticAddress slice)
        {
            var qualifiers = reference.Split('.')[..^1];
            var scope = slice.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key).ToArray();

            return qualifiers.Length <= scope.Length && qualifiers.SequenceEqual(scope.TakeLast(qualifiers.Length));
        }

        SemanticSpecification? BindSpecification(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands)
        {
            var origin = expansion.Specifications.SingleOrDefault(item => ReferenceEquals(item.Effective, specification));
            if (specification.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' description is authoring metadata.", specification.Location);
            }

            if (specification.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' file reference is realization provenance.", specification.File.Location);
            }

            if (specification.ThenNoEvents && (specification.WhenAppended is not null || specification.ThenEvents.Any() ||
                specification.ThenEventsInAnyOrder || specification.ThenErrors.Any() || specification.ThenDenied is not null))
            {
                Error(
                    DiagnosticCodes.InvalidNoEventsExpectation,
                    "'then no events' cannot follow 'when append' or accompany event, event-order, error or denial expectations.",
                    specification.DirectiveLocations.GetValueOrDefault("then no events", specification.Location));
                return null;
            }

            // Performing a query and asserting its results says what 'then query' says, so it binds to exactly the
            // same model - its bytes are those of the 'then query' spelling.
            if (specification.WhenQuery is { } performed)
            {
                var results = specification.ThenResults.ToList();
                if (results.Select(result => result.Exactly).Distinct().Count() > 1)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification '{specification.Name}' compares some results exactly and some not; the executable model compares all of a query's results one way.", specification.Location);
                    return null;
                }

                specification = specification with
                {
                    ThenQueries = [.. specification.ThenQueries, new SpecificationQuerySyntax(performed.Query, performed.Arguments, results, performed.Location) { Exactly = results.Count > 0 && results[0].Exactly }]
                };
            }

            SemanticCommand? command = null;
            if (specification.When is not null &&
                (!commands.TryGetValue(ShortName(specification.When.CommandType), out command) ||
                !CommandBelongsToSlice(specification.When.CommandType, slice)))
            {
                var message = specification.When.CommandType.Contains('.', StringComparison.Ordinal)
                    ? $"Specification '{specification.Name}' command '{specification.When.CommandType}' is unresolved in its slice."
                    : $"Specification '{specification.Name}' command is unresolved in its slice.";
                Error(DiagnosticCodes.InvalidSemanticBinding, message, specification.When.Location);
                return null;
            }

            ValidateSpecificationCompleteness(specification, command);

            var acted = specification.When is not null || specification.WhenAppended is not null || specification.WhenClock is not null ||
                specification.WhenTrigger is not null || specification.WhenCapture is not null;
            var deniedQuery = specification.ThenDenied is not null && !acted &&
                specification.ThenQueries.Count() == 1 && !specification.ThenQueries.Single().Results.Any();
            if (!acted && (specification.ThenEvents.Any() || specification.ThenErrors.Any() ||
                (specification.ThenDenied is not null && !deniedQuery) ||
                (!(specification.ThenReadModels?.Any() ?? false) && !specification.ThenAbsentReadModels.Any() && !specification.ThenQueries.Any())))
            {
                Error(DiagnosticCodes.InvalidWhenlessSpecification, "A specification without 'when' requires 'then readmodel', 'then no readmodel', or 'then query'; denial requires one query with arguments and no results.", specification.Location);
            }

            if (specification.ThenDenied is not null && (specification.ThenErrors.Any() || specification.ThenEvents.Any() ||
                (specification.ThenReadModels?.Any() ?? false) || specification.ThenAbsentReadModels.Any() || (specification.ThenQueries.Any() && !deniedQuery)))
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
            if (origin is not null && (origin.Case is not null || origin.Steps.Any(step => step.Example is not null))) _specificationOrigins.TryAdd(id, origin);
            var givenEvents = specification.Given.Select(value => BindSpecificationEvent(value, commands, historicalFact: true)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var givenReadModels = (specification.GivenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location, value.Exactly))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var when = specification.When is null ? null : BindRoutedSpecificationCommand(specification.When, command!);
            var thenEvents = specification.ThenEvents.Select(value => BindSpecificationEvent(value, commands, expectedFact: true)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenReadModels = (specification.ThenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location, value.Exactly))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var thenQueries = specification.ThenQueries.Select(BindSpecificationQuery).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var thenAbsentReadModels = specification.ThenAbsentReadModels.Select(BindAbsentReadModel).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            if (specification.ThenAbsentReadModels.Any()) UsesV5 = true;
            if (specification.ThenErrors.Count() > 1)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A rejection specification must contain exactly one 'then error' and no success outcomes.", specification.Location);
            }

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
                ThenReturns = BindThenReturns(specification.ThenReturns, command),
                WhenAppended = specification.WhenAppended is null ? null : BindSpecificationAppend(specification.WhenAppended, commands),
                ThenEventsInAnyOrder = specification.ThenEventsInAnyOrder,
                ThenAbsentReadModels = thenAbsentReadModels,
                GivenClock = specification.GivenClock is null ? null : BindClock(specification.GivenClock),
                GivenCaptures = [.. specification.GivenCaptures.Select(BindSpecificationCapture).OfType<SemanticSpecificationCapture>()],
                WhenClock = specification.WhenClock is null ? null : BindClock(specification.WhenClock),
                WhenTrigger = specification.WhenTrigger is null ? null : BindSpecificationTrigger(specification.WhenTrigger),
                WhenCapture = specification.WhenCapture is null ? null : BindSpecificationCapture(specification.WhenCapture)
            };
        }

        void ValidateSpecificationCompleteness(SpecificationSyntax specification, SemanticCommand? command)
        {
            foreach (var step in specification.Given.Concat(specification.ThenEvents).Concat(specification.WhenAppended is { } append ? [append] : []))
            {
                if (_events.TryGetValue(ShortName(step.EventType), out var declaration))
                {
                    ValidateCompleteStep(step, step.EventType, step.Values, declaration.Properties.Values);
                }
            }

            foreach (var step in specification.GivenReadModels ?? [])
            {
                if (_readModels.TryGetValue(ShortName(step.Name), out var declaration))
                {
                    ValidateCompleteStep(step, step.Name, step.Properties, declaration.Properties.Values);
                }
            }

            if (specification.When is { } when && command is not null)
            {
                ValidateCompleteStep(when, when.CommandType, when.Values, command.Properties.Where(property => !property.IsGenerated));
            }
        }

        void ValidateCompleteStep(SyntaxNode step, string type, IEnumerable<PropertyMappingSyntax> values, IEnumerable<SemanticProperty> properties)
        {
            var supplied = values.Select(value => value.Property).ToHashSet(StringComparer.Ordinal);
            var missing = properties.Where(property => !supplied.Contains(property.Name)).ToArray();
            var origin = steps.GetValueOrDefault(step);
            var example = origin?.Example is { } declaration ? $" using example '{declaration.Name}'" : string.Empty;
            foreach (var property in missing)
            {
                Error(DiagnosticCodes.MissingSpecificationProperty, $"Specification '{origin?.Role ?? "step"}'{example} for '{type}' is missing required property '{property.Name}'; supply it in the example or step.", step.Location);
            }
        }

        SemanticSpecificationAppend? BindSpecificationAppend(SpecificationEventSyntax value, Dictionary<string, SemanticCommand> commands)
        {
            var bound = BindSpecificationEvent(value, commands, historicalFact: false);
            return bound is null ? null : new(bound.EventContract, bound.Values) { EventSource = bound.EventSource, Route = bound.Route };
        }

        SemanticSpecificationEvent? BindSpecificationEvent(SpecificationEventSyntax value, Dictionary<string, SemanticCommand> commands, bool historicalFact = false, bool expectedFact = false)
        {
            if (value.NoStream is not null && (!expectedFact || value.Stream is not null))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "'no stream' is only valid on a then event and cannot accompany a route.", value.NoStream.Location);
            }
            if (!expectedFact && value.Stream is not null && value.For is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A routed given or when append event requires 'for <literal>'.", value.Stream.Location);
            }
            if (!_events.TryGetValue(ShortName(value.EventType), out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification event '{value.EventType}' is unresolved.", value.Location);
                return null;
            }

            foreach (var entry in value.Values)
            {
                if (@event.Properties.ContainsKey(entry.Property)) continue;
                var prior = @event.Contract.PriorRevisions.LastOrDefault(revision => revision.Properties.Any(property => property.Name == entry.Property));
                if (prior is null) continue;
                Error(
                    historicalFact ? DiagnosticCodes.UnsupportedEventGenerationSemantics : DiagnosticCodes.InvalidSemanticBinding,
                    historicalFact
                        ? $"Event '{@event.Syntax.Name}' revision {prior.Revision.Value} has property '{entry.Property}', but historical-shape references are unsupported; current revision {@event.Contract.Revision.Value} does not declare it."
                        : $"Specification event property '{entry.Property}' is unresolved on current revision {@event.Contract.Revision.Value} of '{@event.Syntax.Name}' (last declared in revision {prior.Revision.Value}).",
                    entry.Location);
                return null;
            }

            var types = commands.Values.SelectMany(command => command.Produces
                .Where(produced => produced.EventContract == @event.Contract.Id)
                .Select(produced => SemanticModelValidator.ProducedEventSourceType(command, produced)))
                .OfType<SemanticTypeReference>().Distinct().ToArray();

            // A StateView slice may specify an append for an event produced by a command in another slice.
            // The event-source type is the producer's command identifier, regardless of the specification's slice.
            var type = types.Length == 1 ? types[0] : null;
            if (type is null && value.For is not null)
            {
                var producerTypes = CommandEventSourceTypes(ShortName(value.EventType));
                type = producerTypes.Length == 1 ? producerTypes[0] : null;
            }

            // Since ESM v6 reactions and captures append events too; their event source types count as well.
            if (type is null && value.For is not null)
            {
                var producerTypes = ProducerEventSourceTypes(ShortName(value.EventType), []);
                type = producerTypes.Length == 1 ? producerTypes[0] : null;
            }

            return new(
                @event.Contract.Id,
                BindPropertyValues(value.Values, @event.Properties, "specification event"))
            {
                EventSource = value.Stream is not null || value.For is null ? null : BindEventSource(value.For, type),
                Route = BindFixtureRoute(value),
                Unrouted = value.NoStream is not null
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
            if (readModel.Syntax.Properties.Count(property => property.IsKey) > 1)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification read model '{readModel.Model.Name}' uses a composite key. See https://github.com/Cratis/Screenplay/issues/599.", location);
                return null;
            }
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

        SemanticSpecificationAbsentReadModel? BindAbsentReadModel(SpecificationAbsentReadModelSyntax value)
        {
            if (!_readModels.TryGetValue(ShortName(value.Name), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification read model '{value.Name}' is unresolved.", value.Location);
                return null;
            }

            if (readModel.Syntax.Properties.Count(property => property.IsKey) > 1)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification read model '{value.Name}' uses a composite key. See https://github.com/Cratis/Screenplay/issues/599.", value.Location);
                return null;
            }
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);
            if (identifier is null) return null; // The declaration has already reported an ambiguous or missing identifier.
            var key = BindConcreteValue(value.Key, identifier.Type, "specification read model key", false);
            return key is null ? null : new(readModel.Model.Id, key);
        }

        SemanticSpecificationQueryResult? BindSpecificationQuery(SpecificationQuerySyntax value)
        {
            if (!_queries.TryGetValue(ShortName(value.Query), out var query))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification query '{value.Query}' is unresolved.", value.Location);
                return null;
            }

            var arguments = value.Arguments.ToArray();
            SemanticValue key;
            if (query.Argument is null)
            {
                if (arguments.Length != 0)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification query '{value.Query}' is unkeyed and must not state arguments.", value.Location);
                    return null;
                }

                key = SemanticValue.Null;
            }
            else if (arguments.Length != 1 || arguments[0].Property != query.Argument.Name || BindConcreteValue(arguments[0].Source, query.Argument.Type, "specification query argument", false) is not { } boundKey)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification query '{value.Query}' must state exactly its keyed argument in Program v1.", value.Location);
                return null;
            }
            else
            {
                key = boundKey;
            }

            var readModel = _readModels.Values.Single(_ => _.Model.Id == query.ReadModel);
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);
            var inferredResultKey = identifier is not null && query.KeyProperty == identifier.Id ? key : null;
            var results = value.Results
                .Select(result => BindReadModelState(readModel, result.Properties, result.Location, inferredResultKey))
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
                return literal.Value is string text && IsDateTime(target) && IsoInstant().IsMatch(text) &&
                    DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant)
                        ? SemanticValue.Text(instant.Offset == TimeSpan.Zero
                            ? instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                            : instant.ToString("O", CultureInfo.InvariantCulture))
                        : BindLiteral(literal);
            }

            // An enumeration member may be written bare or qualified by its concept, exactly as the compiler accepts
            // it - both name the same member as its quoted spelling and bind to the same value.
            if (expression is PathExpressionSyntax path && EnumerationMember(path.Path, target) is { } member)
            {
                return SemanticValue.Text(member);
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

            // A property declared more than once cannot be bound; it is reported where a value names it.
            var declared = declaration.Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single() : null, StringComparer.Ordinal);
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

                if (property is null)
                {
                    Error(DiagnosticCodes.UnknownStructuredValueMember, $"Property '{member.Name}' is declared more than once on '{declaration.Name}', so the structured value cannot bind it.", member.Location);
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

        // An instant may be written the way people write one - "2026-10-05T08:00:00Z" - and binds to the same
        // round-trip value as its fully written form, so the canonical bytes do not depend on the spelling.
        bool IsDateTime(SemanticTypeReference target) => target.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => target.Primitive == SemanticPrimitiveType.DateTime,
            SemanticTypeReferenceKind.Concept => ConceptShape(target.Target).Primitive == SemanticPrimitiveType.DateTime,
            _ => false
        };

        string? EnumerationMember(string path, SemanticTypeReference target)
        {
            if (target.Kind != SemanticTypeReferenceKind.Concept)
            {
                return null;
            }

            var concept = syntax.Concepts.First(_ => _concepts[_.Name].Id == target.Target);
            if (!concept.IsEnum)
            {
                return null;
            }

            var separator = path.LastIndexOf('.');
            var member = separator < 0 ? path : path[(separator + 1)..];
            var qualifier = separator < 0 ? null : path[..separator];
            return (qualifier is null || string.Equals(qualifier, concept.Name, StringComparison.Ordinal)) && concept.Values.Contains(member, StringComparer.Ordinal)
                ? member
                : null;
        }

        SemanticValue? InvalidShape(ExpressionSyntax expression, string description, string expected)
        {
            Error(DiagnosticCodes.IncompatibleStructuredValue, $"The {description} value must be {expected} for its declared type.", expression.Location);
            return null;
        }
    }
}
