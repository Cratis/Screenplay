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
        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.NonBacktracking)]
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

            if (!ValidateClockSpecificationAdmission(specification)) return null;

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

            var opaque = specification.ThenQueries.FirstOrDefault(value => ReachesOpaqueQuery(slice, value));
            if (opaque is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' reads query '{opaque.Query}', which code performs, so it is not an executable specification.", opaque.Location);
                return null;
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

            ValidateSpecificationCompleteness(slice, specification, command, origin!);

            var acted = HasSpecificationAction(specification);
            var deniedQuery = specification.ThenDenied is not null && !acted &&
                specification.ThenQueries.Count() == 1 && !specification.ThenQueries.Single().Results.Any();
            if (!acted && RequiresSpecificationAction(specification, deniedQuery))
            {
                Error(DiagnosticCodes.InvalidWhenlessSpecification, "A specification without 'when' requires 'then readmodel', 'then no readmodel', or 'then query'; denial requires one query with arguments and no results.", specification.Location);
            }

            if (specification.ThenDenied is not null && (specification.ThenErrors.Any() || specification.ThenEvents.Any() ||
                (specification.ThenReadModels?.Any() ?? false) || specification.ThenAbsentReadModels.Any() || (specification.ThenQueries.Any() && !deniedQuery)))
            {
                Error(DiagnosticCodes.InvalidSpecificationDenied, "'then denied' cannot be combined with success or error outcomes.", specification.ThenDenied.Location);
            }

            var queries = specification.ThenQueries.Select(value => (Syntax: value, Query: ResolveQuery(slice, value)))
                .Where(value => value.Query is not null).ToArray();
            if ((RequiresCaller(command?.Authorization) || queries.Any(value => RequiresCaller(value.Query!.Authorization))) && specification.GivenCaller is null)
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
            var thenQueries = queries.Select(value => BindSpecificationQuery(value.Syntax, value.Query!)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
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
                WhenRedelivered = BindRedelivery(slice, specification),
                ThenNoEvents = specification.ThenNoEvents,
                ThenEventsInAnyOrder = specification.ThenEventsInAnyOrder,
                ThenAbsentReadModels = thenAbsentReadModels,
                GivenClock = specification.GivenClock is null ? null : BindClock(specification.GivenClock),
                GivenCaptures = [.. specification.GivenCaptures.Select(BindSpecificationCapture).OfType<SemanticSpecificationCapture>()],
                GivenGenerated = [.. specification.GivenGenerated.Select(BindSpecificationGenerated).OfType<SemanticSpecificationGeneratedValue>()],
                WhenClock = specification.WhenClock is null ? null : BindClock(specification.WhenClock),
                WhenTrigger = specification.WhenTrigger is null ? null : BindSpecificationTrigger(specification.WhenTrigger),
                WhenCapture = specification.WhenCapture is null ? null : BindSpecificationCapture(specification.WhenCapture),
                ThenInvocations = [.. specification.ThenInvocations.Select(expected => BindSpecificationInvocation(specification, expected)).OfType<SemanticSpecificationInvocation>()]
            };
        }

        void ValidateSpecificationCompleteness(SemanticAddress slice, SpecificationSyntax specification, SemanticCommand? command, EffectiveSpecification expanded)
        {
            foreach (var step in specification.Given.Concat(specification.ThenEvents).Concat(specification.WhenAppended is { } append ? [append] : []))
            {
                if (_events.TryGetValue(ShortName(step.EventType), out var declaration))
                {
                    ValidateCompleteStep(slice, expanded, step, step.EventType, step.Values, declaration.Properties.Values);
                }
            }

            foreach (var step in specification.GivenReadModels ?? [])
            {
                if (_readModels.TryGetValue(ShortName(step.Name), out var declaration))
                {
                    ValidateCompleteStep(slice, expanded, step, step.Name, step.Properties, declaration.Properties.Values);
                }
            }

            if (specification.When is { } when && command is not null)
            {
                ValidateCompleteStep(slice, expanded, when, when.CommandType, when.Values, command.Properties.Where(property => !property.IsGenerated));
            }
        }

        void ValidateCompleteStep(SemanticAddress slice, EffectiveSpecification expanded, SyntaxNode step, string type, IEnumerable<PropertyMappingSyntax> values, IEnumerable<SemanticProperty> properties)
        {
            var supplied = values.Select(value => value.Property).ToHashSet(StringComparer.Ordinal);
            var missing = properties.Where(property => !supplied.Contains(property.Name)).ToArray();
            var origin = steps.GetValueOrDefault(step);
            var example = origin?.Example is { } declaration ? $" using example '{declaration.Name}'" : string.Empty;
            if (missing.Length == 0) return;
            var authored = expanded.Authored;
            var path = slice.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key).Append(authored.Name);
            var arguments = new DiagnosticArguments.Builder()
                .Add("specification", DiagnosticArgument.Declaration("specification", path))
                .Add("step", DiagnosticArgument.Keyword(origin!.Role))
                .Add("stepIndex", DiagnosticArgument.Index(SpecificationStepPositions.IndexOf(authored, origin.Authored.Location)))
                .Add("type", DiagnosticArgument.Text(type)).Build();
            if (origin.Example is { } used) arguments = arguments.With("example", DiagnosticArgument.Text(used.Name));
            if (expanded.Case is { } row) arguments = arguments.With("case", DiagnosticArgument.Text(row.Name));
            foreach (var property in missing)
            {
                Error(DiagnosticCodes.MissingSpecificationProperty, $"Specification '{authored.Name}'{example} for '{type}' is missing required property '{property.Name}'; supply it in the example or step.", step.Location, arguments.With("property", DiagnosticArgument.Text(property.Name)));
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

            if (type is null && @event.Contract.Origin is not null && value.For is not null)
            {
                type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
                if (BindConcreteValue(value.For, type, "foreign event source", false) is not SemanticTextValue source || string.IsNullOrWhiteSpace(source.Value))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, "A foreign event source fixture requires non-empty text.", value.For.Location);
                    return null;
                }
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
            var bound = BindPropertyValues(values, readModel.Properties, "specification read model");
            if (readModel.Model.Properties.Count(property => property.IsIdentifier) > 1)
            {
                var parts = BindReadModelStateKey(readModel, bound, location, inferredKey);
                return parts is null ? null : new(readModel.Model.Id, parts, bound);
            }
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
                Error(DiagnosticCodes.MissingSpecificationReadModelIdentifier, $"Specification read model '{readModel.Model.Name}' must state its identifier property '{identifier.Name}' in this block.", location, DiagnosticArguments.Empty.With("readmodel", DiagnosticArgument.Text(readModel.Model.Name)).With("property", DiagnosticArgument.Text(identifier.Name)));
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

            if (readModel.Model.Properties.Count(property => property.IsIdentifier) > 1)
            {
                var parts = BindAbsentReadModelKey(readModel, value.Key, value.Location);
                return parts is null ? null : new(readModel.Model.Id, parts);
            }
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);
            if (identifier is null) return null; // The declaration has already reported an ambiguous or missing identifier.
            var key = BindConcreteValue(value.Key, identifier.Type, "specification read model key", false);
            return key is null ? null : new(readModel.Model.Id, key);
        }

        SemanticSpecificationQueryResult? BindSpecificationQuery(SpecificationQuerySyntax value, SemanticKeyedQuery query)
        {
            var parameters = query.Filters.Select(filter => new SemanticReadModelQueryArgument(filter.Id, filter.Name, filter.Type))
                .Concat(query.Argument is null ? [] : new[] { query.Argument })
                .Concat(query.KeyParts.Select(part => new SemanticReadModelQueryArgument(part.Id, part.Name, part.Type)))
                .ToDictionary(argument => argument.Name, StringComparer.Ordinal);
            var arguments = new Dictionary<string, SemanticValue>(StringComparer.Ordinal);
            foreach (var argument in value.Arguments)
            {
                if (!parameters.TryGetValue(argument.Property, out var parameter) || arguments.ContainsKey(argument.Property))
                {
                    Error(DiagnosticCodes.UnknownSpecificationQueryArgument, $"Specification query '{value.Query}' argument '{argument.Property}' is unknown or repeated.", argument.Location, DiagnosticArguments.Empty.With("query", DiagnosticArgument.Text(value.Query)).With("argument", DiagnosticArgument.Text(argument.Property)));
                    return null;
                }
                if (BindConcreteValue(argument.Source, parameter.Type, "specification query argument", false) is not { } concrete) return null;
                arguments.Add(argument.Property, concrete);
            }

            foreach (var parameter in parameters.Values.Where(parameter => parameter == query.Argument || !parameter.Type.IsOptional))
            {
                if (!arguments.ContainsKey(parameter.Name))
                {
                    Error(DiagnosticCodes.UnknownSpecificationQueryArgument, $"Specification query '{value.Query}' required argument '{parameter.Name}' is missing.", value.Location, DiagnosticArguments.Empty.With("query", DiagnosticArgument.Text(value.Query)).With("argument", DiagnosticArgument.Text(parameter.Name)));
                    return null;
                }
            }

            var key = query.Argument is null ? SemanticValue.Null : arguments[query.Argument.Name];
            if (!query.KeyParts.IsEmpty)
            {
                key = SemanticValue.Composite([.. query.KeyParts.Select(part => new SemanticPropertyValue(part.KeyProperty, arguments[part.Name]))]);
            }
            var filters = query.Filters.Where(filter => arguments.ContainsKey(filter.Name))
                .Select(filter => new SemanticQueryFilterValue(filter.Id, arguments[filter.Name])).ToImmutableArray();

            var readModel = _readModels.Values.Single(_ => _.Model.Id == query.ReadModel);
            var identifiers = readModel.Model.Properties.Where(property => property.IsIdentifier).ToArray();
            var inferredResultKey = !query.KeyParts.IsEmpty || (identifiers.Length == 1 && query.KeyProperty == identifiers[0].Id) ? key : null;
            var results = value.Results
                .Select(result => BindReadModelState(readModel, result.Properties, result.Location, inferredResultKey))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(query.Id, key, results) { Exactly = value.Exactly, Filters = filters };
        }

        ImmutableArray<SemanticPropertyValue> BindPropertyValues(
            IEnumerable<PropertyMappingSyntax> values,
            Dictionary<string, SemanticProperty> properties,
            string description,
            IReadOnlySet<SemanticId>? routedInputs = null)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            foreach (var value in values)
            {
                if (!properties.TryGetValue(value.Property, out var property))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"The {description} property '{value.Property}' is unresolved.", value.Location);
                    continue;
                }

                if (BindConcreteValue(value.Source, property.Type, description, description == "specification read model", routedInputs?.Contains(property.Id) == true) is { } concrete)
                {
                    bound.Add(new(property.Id, concrete));
                }
            }

            return bound.ToImmutable();
        }

        SemanticValue? BindConcreteValue(ExpressionSyntax expression, SemanticTypeReference target, string description, bool allowNull, bool routeInput = false)
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
                if (literal.Value is string text && IsDateTime(target) && IsoInstant().IsMatch(text) &&
                    DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
                {
                    return SemanticValue.Text(instant.Offset == TimeSpan.Zero
                        ? instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                        : instant.ToString("O", CultureInfo.InvariantCulture));
                }

                return routeInput ? BindRouteLiteral(literal) : BindLiteral(literal);
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
