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
        internal CompilationResult<SemanticCompilation> CompleteCompilation(
            SemanticCompilation compilation,
            ImmutableArray<SemanticTypedContextDescriptor> descriptors,
            ImmutableArray<SemanticTypedContextDescriptor> failureDescriptors)
        {
            if (HasErrors)
            {
                return new(null, Diagnostics)
                {
                    SpecificationRefusedValue = compilation,
                    ImplementationRequirements = ImplementationRequirements,
                    TypedContextDescriptors = failureDescriptors
                };
            }

            return new(compilation, Diagnostics)
            {
                ImplementationRequirements = ImplementationRequirements,
                TypedContextDescriptors = DescriptorsFor(compilation, descriptors)
            };
        }

        static IEnumerable<SliceSyntax> AllSlices(FeatureSyntax feature) =>
            feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

        static bool CommandBelongsToSlice(string reference, SemanticAddress slice)
        {
            var qualifiers = reference.Split('.')[..^1];
            var scope = SpecificationSlicePath(slice).ToArray();

            return qualifiers.Length <= scope.Length && qualifiers.SequenceEqual(scope.TakeLast(qualifiers.Length));
        }

        static IEnumerable<string> SpecificationSlicePath(SemanticAddress slice) =>
            slice.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key);

        static bool IsDeniedSpecificationQuery(SpecificationSyntax specification, bool acted) =>
            specification.ThenDenied is not null && !acted &&
            specification.ThenQueries.Count() == 1 && !specification.ThenQueries.Single().Results.Any();

        static bool HasConflictingDenialOutcome(SpecificationSyntax specification, bool deniedQuery) =>
            specification.ThenErrors.Any() || specification.ThenEvents.Any() ||
            (specification.ThenReadModels?.Any() ?? false) || specification.ThenAbsentReadModels.Any() ||
            (specification.ThenQueries.Any() && !deniedQuery);

        static SemanticCaller? BindSpecificationCaller(SpecificationCallerSyntax? caller) => caller is null ? null : new(
            caller.Authenticated,
            [.. caller.Roles],
            [.. caller.Claims.Select(claim => new SemanticCallerClaim(claim.Type, claim.Value))])
        {
            Id = caller.Id
        };

        static SemanticTypeReference? UniqueSpecificationSourceType(SemanticTypeReference[] types) => types.Length == 1 ? types[0] : null;

        static bool HasUnroutedEventConflict(SpecificationEventSyntax value, SpecificationEventRole role) => role != SpecificationEventRole.Expected || value.Stream is not null;

        SemanticSpecification? BindSpecification(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands)
        {
            var origin = expansion.Specifications.SingleOrDefault(item => ReferenceEquals(item.Effective, specification));
            var id = documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, specification.Name));
            _specificationBindings[specification] = (id, origin);
            var diagnosticStart = _diagnostics.Count;
            var sourceStart = _sourceMapEntries.Count;
            var versions = (UsesV2, UsesV5, UsesV6, UsesV7, UsesV30);
            try
            {
                return BindSpecificationCore(slice, specification, commands, origin);
            }
            finally
            {
                (UsesV2, UsesV5, UsesV6, UsesV7, UsesV30) = versions;
                _specificationSources[id] = [.. _sourceMapEntries.Skip(sourceStart).Select(entry => entry.SemanticId)];
                RecordSpecificationErrors(id, origin, diagnosticStart);
            }
        }

        SemanticSpecification? BindSpecificationCore(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands,
            EffectiveSpecification? origin)
        {
            ReportSpecificationMetadata(specification);
            if (!ValidateClockSpecificationAdmission(specification))
            {
                return null;
            }

            if (NormalisePerformedQuery(specification) is not { } normalised)
            {
                return null;
            }

            if (ReadsOpaqueSpecificationQuery(slice, normalised))
            {
                return null;
            }

            if (!TryResolveSpecificationCommand(slice, normalised, commands, out var command))
            {
                return null;
            }

            ValidateSpecificationCompleteness(slice, normalised, command, origin!);
            ValidateSpecificationAction(normalised);
            var queries = normalised.ThenQueries.Select(value => (Syntax: value, Query: ResolveQuery(slice, value)))
                .Where(value => value.Query is not null).ToArray();
            ValidateSpecificationCaller(normalised, command, queries);

            var id = Resolve(SemanticAddress.ForSpecification(slice, normalised.Name), normalised.Location);
            RecordSpecificationOrigin(id, origin);
            return BindSpecificationSteps((slice, id), normalised, commands, command, queries);
        }

        void ReportSpecificationMetadata(SpecificationSyntax specification)
        {
            if (specification.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' description is authoring metadata.", specification.Location);
            }

            if (specification.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' file reference is realization provenance.", specification.File.Location);
            }
        }

        bool ReadsOpaqueSpecificationQuery(SemanticAddress slice, SpecificationSyntax specification)
        {
            var opaque = specification.ThenQueries.FirstOrDefault(value => ReachesOpaqueQuery(slice, value));
            if (opaque is null)
            {
                return false;
            }

            Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' reads query '{opaque.Query}', which code performs, so it is not an executable specification.", opaque.Location);
            return true;
        }

        bool TryResolveSpecificationCommand(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands,
            out SemanticCommand? command)
        {
            command = null;
            if (specification.When is not { } when)
            {
                return true;
            }

            if (commands.TryGetValue(ShortName(when.CommandType), out command) && CommandBelongsToSlice(when.CommandType, slice))
            {
                return true;
            }

            var message = when.CommandType.Contains('.', StringComparison.Ordinal)
                ? $"Specification '{specification.Name}' command '{when.CommandType}' is unresolved in its slice."
                : $"Specification '{specification.Name}' command is unresolved in its slice.";
            Error(DiagnosticCodes.InvalidSemanticBinding, message, when.Location);
            return false;
        }

        void ValidateSpecificationAction(SpecificationSyntax specification)
        {
            var acted = HasSpecificationAction(specification);
            var deniedQuery = IsDeniedSpecificationQuery(specification, acted);
            if (!acted && RequiresSpecificationAction(specification, deniedQuery))
            {
                Error(DiagnosticCodes.InvalidWhenlessSpecification, "A specification without 'when' requires 'then readmodel', 'then no readmodel', or 'then query'; denial requires one query with arguments and no results.", specification.Location);
            }

            if (specification.ThenDenied is { } denial && HasConflictingDenialOutcome(specification, deniedQuery))
            {
                Error(DiagnosticCodes.InvalidSpecificationDenied, "'then denied' cannot be combined with success or error outcomes.", denial.Location);
            }
        }

        void ValidateSpecificationCaller(
            SpecificationSyntax specification,
            SemanticCommand? command,
            (SpecificationQuerySyntax Syntax, SemanticKeyedQuery? Query)[] queries)
        {
            var needsCaller = RequiresCaller(command?.Authorization) || queries.Any(value => RequiresCaller(value.Query!.Authorization));
            if (needsCaller && specification.GivenCaller is null)
            {
                Error(DiagnosticCodes.MissingSpecificationCaller, "An authorized specification requires an explicit 'given caller' fixture; missing identity context is never assumed.", specification.Location);
            }
        }

        void RecordSpecificationOrigin(SemanticId id, EffectiveSpecification? origin)
        {
            if (origin is null)
            {
                return;
            }

            if (origin.Case is not null || origin.Steps.Any(step => step.Example is not null))
            {
                _specificationOrigins.TryAdd(id, origin);
            }
        }

        SemanticSpecification BindSpecificationSteps(
            (SemanticAddress Slice, SemanticId Id) address,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands,
            SemanticCommand? command,
            (SpecificationQuerySyntax Syntax, SemanticKeyedQuery? Query)[] queries)
        {
            var givenEvents = specification.Given.Select(value => BindSpecificationEventStep(value, commands, SpecificationEventRole.Historical)).OfType<SemanticSpecificationEvent>().ToImmutableArray();
            var givenReadModels = BindSpecificationReadModels(specification.GivenReadModels);
            var when = specification.When is null ? null : BindRoutedSpecificationCommand(specification.When, command!);
            var thenEvents = specification.ThenEvents.Select(value => BindSpecificationEventStep(value, commands, SpecificationEventRole.Expected)).OfType<SemanticSpecificationEvent>().ToImmutableArray();
            var thenReadModels = BindSpecificationReadModels(specification.ThenReadModels);
            var thenQueries = queries.Select(value => BindSpecificationQuery(value.Syntax, value.Query!)).OfType<SemanticSpecificationQueryResult>().ToImmutableArray();
            var thenAbsentReadModels = BindAbsentReadModels(specification);
            var thenErrors = BindSpecificationErrors(specification);
            return new(address.Id, specification.Name, givenEvents, givenReadModels, when, thenEvents, thenReadModels, thenQueries, thenErrors)
            {
                GivenCaller = BindSpecificationCaller(specification.GivenCaller),
                ThenDenied = specification.ThenDenied is not null,
                ThenReturns = BindThenReturns(specification.ThenReturns, command),
                WhenAppended = specification.WhenAppended is null ? null : BindSpecificationAppend(specification.WhenAppended, commands),
                WhenRedelivered = BindRedelivery(address.Slice, specification),
                ThenNoEvents = specification.ThenNoEvents,
                ThenEventsInAnyOrder = specification.ThenEventsInAnyOrder,
                ThenAbsentReadModels = thenAbsentReadModels,
                GivenClock = specification.GivenClock is null ? null : BindClock(specification.GivenClock),
                GivenTimeZone = specification.GivenTimeZone?.Id,
                GivenCaptures = [.. specification.GivenCaptures.Select(BindSpecificationCapture).OfType<SemanticSpecificationCapture>()],
                GivenAbilityAnswers = [.. specification.GivenAbilityAnswers.Select(BindAbilityAnswer).OfType<SemanticAbilityAnswer>()],
                GivenGenerated = [.. specification.GivenGenerated.Select(BindSpecificationGenerated).OfType<SemanticSpecificationGeneratedValue>()],
                WhenClock = specification.WhenClock is null ? null : BindClock(specification.WhenClock),
                WhenTrigger = specification.WhenTrigger is null ? null : BindSpecificationTrigger(specification.WhenTrigger),
                WhenCapture = specification.WhenCapture is null ? null : BindSpecificationCapture(specification.WhenCapture),
                ThenInvocations = [.. specification.ThenInvocations.Select(expected => BindSpecificationInvocation(specification, expected)).OfType<SemanticSpecificationInvocation>()]
            };
        }

        ImmutableArray<SemanticSpecificationReadModel> BindSpecificationReadModels(IEnumerable<SpecificationReadModelSyntax>? states) =>
            (states ?? []).Select(value => BindReadModelState(value.Name, value.Properties, value.Location, value.Exactly)).OfType<SemanticSpecificationReadModel>().ToImmutableArray();

        ImmutableArray<SemanticSpecificationAbsentReadModel> BindAbsentReadModels(SpecificationSyntax specification)
        {
            var absent = specification.ThenAbsentReadModels.Select(BindAbsentReadModel).OfType<SemanticSpecificationAbsentReadModel>().ToImmutableArray();
            if (specification.ThenAbsentReadModels.Any())
            {
                UsesV5 = true;
            }

            return absent;
        }

        ImmutableArray<SemanticSpecificationError> BindSpecificationErrors(SpecificationSyntax specification)
        {
            if (specification.ThenErrors.Count() > 1)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A rejection specification must contain exactly one 'then error' and no success outcomes.", specification.Location);
            }

            return specification.ThenErrors.Select(value =>
            {
                ValidateStringKey(value.Name, value.Location);
                return new SemanticSpecificationError(null, value.Name);
            }).ToImmutableArray();
        }

        // Performing a query and asserting its results says what 'then query' says, so it binds to exactly the
        // same model - its bytes are those of the 'then query' spelling. Returns null once the refusal is reported.
        SpecificationSyntax? NormalisePerformedQuery(SpecificationSyntax specification)
        {
            if (specification.WhenQuery is not { } performed)
            {
                return specification;
            }

            var results = specification.ThenResults.ToList();
            if (results.Select(result => result.Exactly).Distinct().Count() > 1)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Specification '{specification.Name}' compares some results exactly and some not; the executable model compares all of a query's results one way.", specification.Location);
                return null;
            }

            return specification with
            {
                ThenQueries = [.. specification.ThenQueries, new SpecificationQuerySyntax(performed.Query, performed.Arguments, results, performed.Location) { Exactly = results.Count > 0 && results[0].Exactly }]
            };
        }

        void ValidateSpecificationCompleteness(SemanticAddress slice, SpecificationSyntax specification, SemanticCommand? command, EffectiveSpecification expanded)
        {
            var eventSteps = specification.Given.Concat(specification.ThenEvents).Concat(specification.WhenAppended is { } append ? [append] : []);
            foreach (var step in eventSteps)
            {
                ValidateCompleteEvent(slice, expanded, step);
            }

            foreach (var step in specification.GivenReadModels ?? [])
            {
                ValidateCompleteReadModel(slice, expanded, step);
            }

            if (specification.When is { } when && command is not null)
            {
                ValidateCompleteStep(slice, expanded, (when, when.CommandType, when.Values), command.Properties.Where(property => !property.IsGenerated));
            }
        }

        void ValidateCompleteEvent(SemanticAddress slice, EffectiveSpecification expanded, SpecificationEventSyntax step)
        {
            if (_events.TryGetValue(ShortName(step.EventType), out var declaration))
            {
                ValidateCompleteStep(slice, expanded, (step, step.EventType, step.Values), declaration.Properties.Values);
            }
        }

        void ValidateCompleteReadModel(SemanticAddress slice, EffectiveSpecification expanded, SpecificationReadModelSyntax step)
        {
            if (_readModels.TryGetValue(ShortName(step.Name), out var declaration))
            {
                ValidateCompleteStep(slice, expanded, (step, step.Name, step.Properties), declaration.Properties.Values);
            }
        }

        void ValidateCompleteStep(
            SemanticAddress slice,
            EffectiveSpecification expanded,
            (SyntaxNode Node, string Type, IEnumerable<PropertyMappingSyntax> Values) step,
            IEnumerable<SemanticProperty> properties)
        {
            var supplied = step.Values.Select(value => value.Property).ToHashSet(StringComparer.Ordinal);
            var missing = properties.Where(property => !supplied.Contains(property.Name)).ToArray();
            var origin = steps.GetValueOrDefault(step.Node);
            var example = origin?.Example is { } declaration ? $" using example '{declaration.Name}'" : string.Empty;
            if (missing.Length == 0)
            {
                return;
            }

            var arguments = MissingSpecificationPropertyArguments(slice, expanded, origin!, step.Type);
            foreach (var property in missing)
            {
                Error(DiagnosticCodes.MissingSpecificationProperty, $"Specification '{expanded.Authored.Name}'{example} for '{step.Type}' is missing required property '{property.Name}'; supply it in the example or step.", step.Node.Location, arguments.With("property", DiagnosticArgument.Text(property.Name)));
            }
        }

        DiagnosticArguments MissingSpecificationPropertyArguments(SemanticAddress slice, EffectiveSpecification expanded, EffectiveSpecificationStep origin, string type)
        {
            var authored = expanded.Authored;
            var path = SpecificationSlicePath(slice).Append(authored.Name);
            var arguments = new DiagnosticArguments.Builder()
                .Add("specification", DiagnosticArgument.Declaration("specification", path))
                .Add("step", DiagnosticArgument.Keyword(origin.Role))
                .Add("stepIndex", DiagnosticArgument.Index(SpecificationStepPositions.IndexOf(authored, origin.Authored.Location)))
                .Add("type", DiagnosticArgument.Text(type)).Build();
            if (origin.Example is { } used)
            {
                arguments = arguments.With("example", DiagnosticArgument.Text(used.Name));
            }

            if (expanded.Case is { } row)
            {
                arguments = arguments.With("case", DiagnosticArgument.Text(row.Name));
            }

            return arguments;
        }

        SemanticSpecificationAppend? BindSpecificationAppend(SpecificationEventSyntax value, Dictionary<string, SemanticCommand> commands)
        {
            var bound = BindSpecificationEventStep(value, commands, SpecificationEventRole.Append);
            return bound is null ? null : new(bound.EventContract, bound.Values) { EventSource = bound.EventSource, Route = bound.Route };
        }

        SemanticSpecificationEvent? BindSpecificationEvent(
            SpecificationEventSyntax value,
            Dictionary<string, SemanticCommand> commands,
            bool historicalFact = false,
            SemanticTypeReference? subjectKey = null) =>
            BindSpecificationEventStep(value, commands, historicalFact ? SpecificationEventRole.Historical : SpecificationEventRole.Append, subjectKey);

        SemanticSpecificationEvent? BindSpecificationEventStep(
            SpecificationEventSyntax value,
            Dictionary<string, SemanticCommand> commands,
            SpecificationEventRole role,
            SemanticTypeReference? subjectKey = null)
        {
            ValidateSpecificationEventRoute(value, role);
            if (!_events.TryGetValue(ShortName(value.EventType), out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification event '{value.EventType}' is unresolved.", value.Location);
                return null;
            }

            if (!ValidateSpecificationEventRevision(value, @event, role == SpecificationEventRole.Historical))
            {
                return null;
            }

            if (!TryBindSpecificationSourceType(value, @event, commands, subjectKey, out var type))
            {
                return null;
            }

            return new(
                @event.Contract.Id,
                BindPropertyValues(value.Values, @event.Properties, "specification event"))
            {
                EventSource = value.Stream is not null || value.For is null ? null : BindEventSource(value.For, type, value.EventType),
                Route = BindFixtureRoute(value),
                Unrouted = value.NoStream is not null,
                ExpectedTags = role == SpecificationEventRole.Expected ? BindExpectedTags(value) : null
            };
        }

        void ValidateSpecificationEventRoute(SpecificationEventSyntax value, SpecificationEventRole role)
        {
            if (value.NoStream is { } noStream && HasUnroutedEventConflict(value, role))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "'no stream' is only valid on a then event and cannot accompany a route.", noStream.Location);
            }

            if (role == SpecificationEventRole.Expected)
            {
                return;
            }

            if (value.Stream is { } stream && value.For is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A routed given or when append event requires 'for <literal>'.", stream.Location);
            }
        }

        bool ValidateSpecificationEventRevision(SpecificationEventSyntax value, BoundEvent @event, bool historicalFact)
        {
            foreach (var entry in value.Values)
            {
                if (@event.Properties.ContainsKey(entry.Property))
                {
                    continue;
                }

                var prior = @event.Contract.PriorRevisions.LastOrDefault(revision => revision.Properties.Any(property => property.Name == entry.Property));
                if (prior is null)
                {
                    continue;
                }

                Error(
                    historicalFact ? DiagnosticCodes.UnsupportedEventGenerationSemantics : DiagnosticCodes.InvalidSemanticBinding,
                    historicalFact
                        ? $"Event '{@event.Syntax.Name}' revision {prior.Revision.Value} has property '{entry.Property}', but historical-shape references are unsupported; current revision {@event.Contract.Revision.Value} does not declare it."
                        : $"Specification event property '{entry.Property}' is unresolved on current revision {@event.Contract.Revision.Value} of '{@event.Syntax.Name}' (last declared in revision {prior.Revision.Value}).",
                    entry.Location);
                return false;
            }

            return true;
        }

        bool TryBindSpecificationSourceType(
            SpecificationEventSyntax value,
            BoundEvent @event,
            Dictionary<string, SemanticCommand> commands,
            SemanticTypeReference? subjectKey,
            out SemanticTypeReference? type)
        {
            var types = commands.Values.SelectMany(command => command.Produces
                .Where(produced => produced.EventContract == @event.Contract.Id)
                .Select(produced => SemanticModelValidator.ProducedEventSourceType(command, produced)))
                .OfType<SemanticTypeReference>().Distinct().ToArray();
            type = UniqueSpecificationSourceType(types);
            if (type is null && value.For is not null)
            {
                // Lifecycle fixtures can fall back to their subject's key if no producer defines one.
                type = FindSpecificationProducerType(ShortName(value.EventType)) ?? subjectKey;
            }

            if (type is null && @event.Contract.Origin is not null && value.For is { } source)
            {
                type = BindForeignSpecificationSourceType(source);
                if (type is null)
                {
                    return false;
                }
            }

            // Producerless history can be typed by the read model keyed on the event's own source identity.
            type = FixtureEventSourceType(type, value.For, ShortName(value.EventType));
            return true;
        }

        // A producer can live in another slice, and reactions and captures can append events too.
        SemanticTypeReference? FindSpecificationProducerType(string eventName) =>
            UniqueSpecificationSourceType(CommandEventSourceTypes(eventName)) ?? UniqueSpecificationSourceType(ProducerEventSourceTypes(eventName, []));

        SemanticTypeReference? BindForeignSpecificationSourceType(ExpressionSyntax expression)
        {
            var type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            if (BindConcreteValue(expression, type, "foreign event source", false) is not SemanticTextValue source || string.IsNullOrWhiteSpace(source.Value))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A foreign event source fixture requires non-empty text.", expression.Location);
                return null;
            }

            return type;
        }

        ImmutableArray<string>? BindExpectedTags(SpecificationEventSyntax value)
        {
            if (value.NoTags is not null)
            {
                return [];
            }

            return value.Tags.Any() ? BindTags(value.Tags) : null;
        }

        SemanticEventSourceIdentity? BindEventSource(ExpressionSyntax expression, SemanticTypeReference? type, string? eventName = null)
        {
            if (type is null or { IsCollection: true } or { IsOptional: true })
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, FixtureEventSourceMessage(eventName), expression.Location);
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
            if (identifier is null)
            {
                return null; // The declaration has already reported an ambiguous or missing identifier.
            }

            var key = BindConcreteValue(value.Key, identifier.Type, "specification read model key", false);
            return key is null ? null : new(readModel.Model.Id, key);
        }

        SemanticSpecificationQueryResult? BindSpecificationQuery(SpecificationQuerySyntax value, SemanticKeyedQuery query)
        {
            if (BindSpecificationQueryArguments(value, query) is not { } arguments)
            {
                return null;
            }

            var key = query.Argument is null ? SemanticValue.Null : arguments[query.Argument.Name];
            if (!query.KeyParts.IsEmpty)
            {
                key = SemanticValue.Composite([.. query.KeyParts.Select(part => new SemanticPropertyValue(part.KeyProperty, arguments[part.Name]))]);
            }

            var filters = query.Filters.Where(filter => arguments.ContainsKey(filter.Name))
                .Select(filter => new SemanticQueryFilterValue(filter.Id, arguments[filter.Name])).ToImmutableArray();
            var results = BindSpecificationQueryResults(value, query, key);
            return new(query.Id, key, results) { Exactly = value.Exactly, Filters = filters };
        }

        Dictionary<string, SemanticValue>? BindSpecificationQueryArguments(SpecificationQuerySyntax value, SemanticKeyedQuery query)
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

                if (BindConcreteValue(argument.Source, parameter.Type, "specification query argument", false) is not { } concrete)
                {
                    return null;
                }

                arguments.Add(argument.Property, concrete);
            }

            return ValidateRequiredSpecificationArguments(value, query, parameters, arguments) ? arguments : null;
        }

        bool ValidateRequiredSpecificationArguments(
            SpecificationQuerySyntax value,
            SemanticKeyedQuery query,
            Dictionary<string, SemanticReadModelQueryArgument> parameters,
            Dictionary<string, SemanticValue> arguments)
        {
            foreach (var parameter in parameters.Values.Where(parameter => parameter == query.Argument || !parameter.Type.IsOptional))
            {
                if (arguments.ContainsKey(parameter.Name))
                {
                    continue;
                }

                Error(DiagnosticCodes.UnknownSpecificationQueryArgument, $"Specification query '{value.Query}' required argument '{parameter.Name}' is missing.", value.Location, DiagnosticArguments.Empty.With("query", DiagnosticArgument.Text(value.Query)).With("argument", DiagnosticArgument.Text(parameter.Name)));
                return false;
            }

            return true;
        }

        ImmutableArray<SemanticSpecificationReadModel> BindSpecificationQueryResults(SpecificationQuerySyntax value, SemanticKeyedQuery query, SemanticValue key)
        {
            var readModel = _readModels.Values.Single(_ => _.Model.Id == query.ReadModel);
            var identifiers = readModel.Model.Properties.Where(property => property.IsIdentifier).ToArray();
            var inferredResultKey = !query.KeyParts.IsEmpty || (identifiers.Length == 1 && query.KeyProperty == identifiers[0].Id) ? key : null;
            return value.Results.Select(result => BindReadModelState(readModel, result.Properties, result.Location, inferredResultKey))
                .OfType<SemanticSpecificationReadModel>().ToImmutableArray();
        }

        enum SpecificationEventRole
        {
            Historical,
            Append,
            Expected
        }
    }
}
