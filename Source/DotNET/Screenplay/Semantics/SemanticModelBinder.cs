// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds compatible source syntax to ESM v1 and reports every non-bound construct explicitly.
/// </summary>
public sealed class SemanticModelBinder : ISemanticModelBinder
{
    /// <inheritdoc/>
    public CompilationResult<SemanticCompilation> Bind(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents)
    {
        var context = new BindingContext(applicationName, syntax, documents);
        try
        {
            var application = context.BindApplication();
            if (context.HasErrors)
            {
                return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
            }

            var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
            var sourceMap = SemanticSourceMap.Create(context.SourceMapEntries, documents.Documents);
            var compilation = SemanticCompilation.Create(model, documents, sourceMap);
            return new(compilation, context.Diagnostics);
        }
        catch (InvalidSemanticContract exception)
        {
            context.Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, syntax.Location);
            return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
        }
    }

    sealed class BindingContext(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        readonly List<Diagnostic> _diagnostics = [];
        readonly List<SemanticSourceMapEntry> _sourceMapEntries = [];
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _concepts = new(StringComparer.Ordinal);
        readonly Dictionary<EventSyntax, BoundEvent> _eventDeclarations = [];
        readonly Dictionary<string, BoundEvent> _events = new(StringComparer.Ordinal);
        readonly Dictionary<QuerySyntax, SemanticKeyedQuery> _queryDeclarations = [];
        readonly Dictionary<string, SemanticKeyedQuery> _queries = new(StringComparer.Ordinal);
        readonly Dictionary<ReadModelSyntax, BoundReadModel> _readModelDeclarations = [];
        readonly Dictionary<string, BoundReadModel> _readModels = new(StringComparer.Ordinal);
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _types = new(StringComparer.Ordinal);
        readonly ApplicationIdentity _applicationIdentity = documents.IdentityCatalog.Application;

        internal IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        internal bool HasErrors => _diagnostics.Exists(_ => _.Severity == DiagnosticSeverity.Error);

        internal ImmutableArray<SemanticSourceMapEntry> SourceMapEntries => [.. _sourceMapEntries];

        internal SemanticApplication BindApplication()
        {
            ReportTopLevelDispositions();
            var applicationAddress = SemanticAddress.ForApplication(_applicationIdentity);
            var applicationId = Resolve(applicationAddress, syntax.Location);

            RegisterTypeDeclarations();
            RegisterEventDeclarations();
            RegisterReadModelDeclarations();
            RegisterQueryDeclarations();
            var concepts = syntax.Concepts.Select(BindConcept).ToImmutableArray();
            var types = (syntax.Types ?? []).Select(BindType).ToImmutableArray();
            var modules = syntax.Modules.Select(BindModule).ToImmutableArray();
            return new(applicationId, applicationName, concepts, types, modules);
        }

        internal void Error(string code, string message, SourceLocation location) =>
            _diagnostics.Add(Diagnostic.Error(code, message, location));

        void Information(string code, string message, SourceLocation location) =>
            _diagnostics.Add(new(DiagnosticSeverity.Information, code, message, location));

        void ReportTopLevelDispositions()
        {
            if (syntax.Domain is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "A domain declaration is authoring metadata and is not part of ESM v1 behavior.", syntax.Domain.Location);
            }

            foreach (var import in syntax.Imports)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Import '{import.QualifiedName}' is not supported by ESM v1 binding.", import.Location);
            }

            foreach (var policy in syntax.Policies)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' is deferred until portable policy semantics are admitted.", policy.Location);
            }

            foreach (var persona in syntax.Personas ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Persona '{persona.Name}' is deferred from ESM v1.", persona.Location);
            }

            if (syntax.Authentication is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Authentication providers are realization metadata and are not part of ESM v1 behavior.", syntax.Authentication.Location);
            }

            foreach (var seed in syntax.Seeds ?? [])
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Event seeding is operational metadata and is not part of ESM v1 behavior.", seed.Location);
            }

            foreach (var trigger in syntax.Triggers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Trigger '{trigger.Name}' is deferred until portable occurrence semantics are admitted.", trigger.Location);
            }

            foreach (var profile in syntax.UiProfiles ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"UI profile '{profile.Name}' is explicitly deferred from the backend ESM v1 profile.", profile.Location);
            }

            foreach (var theme in syntax.Themes ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Theme '{theme.Name}' is explicitly deferred from the backend ESM v1 profile.", theme.Location);
            }

            foreach (var layout in syntax.Layouts ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Layout '{layout.Name}' is explicitly deferred from the backend ESM v1 profile.", layout.Location);
            }
        }

        void RegisterTypeDeclarations()
        {
            foreach (var concept in syntax.Concepts)
            {
                var address = SemanticAddress.ForConcept(_applicationIdentity, concept.Name);
                _concepts[concept.Name] = (address, Resolve(address, concept.Location));
            }

            foreach (var type in syntax.Types ?? [])
            {
                var address = SemanticAddress.ForCompositeType(_applicationIdentity, type.Name);
                _types[type.Name] = (address, Resolve(address, type.Location));
            }
        }

        void RegisterEventDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var @event in slice.Events)
                {
                    var bound = BindEvent(sliceAddress, @event);
                    _eventDeclarations.Add(@event, bound);
                    if (!_events.TryAdd(@event.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event reference '{@event.Name}' is ambiguous across slices in the current ESM v1 binder.", @event.Location);
                    }
                }
            }
        }

        void RegisterReadModelDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var readModel in slice.ReadModels ?? [])
                {
                    var bound = BindReadModel(sliceAddress, slice, readModel);
                    _readModelDeclarations.Add(readModel, bound);
                    if (!_readModels.TryAdd(readModel.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Read model reference '{readModel.Name}' is ambiguous across slices in the current ESM v1 binder.", readModel.Location);
                    }
                }
            }
        }

        void RegisterQueryDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var query in slice.Queries)
                {
                    var bound = BindQuery(sliceAddress, query);
                    if (bound is null)
                    {
                        continue;
                    }

                    _queryDeclarations.Add(query, bound);
                    if (!_queries.TryAdd(query.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query reference '{query.Name}' is ambiguous across slices in the current ESM v1 binder.", query.Location);
                    }
                }
            }
        }

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

            var identifier = identifierNames.SingleOrDefault();
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

            if (query.IsObservable || query.Filters.Any() || query.Scope is not null || query.Authorize is not null || query.Performer is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query '{query.Name}' uses delivery, filtering, scope, authorization, or implementation behavior outside the first ESM v1 vertical.", query.Location);
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

        IEnumerable<(string Module, ImmutableArray<string> FeaturePath, SliceSyntax Slice)> AllSlices()
        {
            foreach (var module in syntax.Modules)
            {
                foreach (var value in AllSlices(module.Name, [], module.Features))
                {
                    yield return value;
                }
            }
        }

        IEnumerable<(string Module, ImmutableArray<string> FeaturePath, SliceSyntax Slice)> AllSlices(
            string module,
            ImmutableArray<string> parentPath,
            IEnumerable<FeatureSyntax> features)
        {
            foreach (var feature in features)
            {
                var path = parentPath.Add(feature.Name);
                foreach (var slice in feature.Slices)
                {
                    yield return (module, path, slice);
                }

                foreach (var nested in AllSlices(module, path, feature.Features))
                {
                    yield return nested;
                }
            }
        }

        SemanticConcept BindConcept(ConceptSyntax concept)
        {
            if (concept.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' file reference is realization provenance.", concept.File.Location);
            }

            if (concept.Attributes.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' compliance attributes require portable data-subject semantics.", concept.Location);
            }

            var primitive = concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type);
            var validations = BindConceptValidations(concept);
            return new(_concepts[concept.Name].Id, concept.Name, primitive, [.. concept.Values], validations);
        }

        ImmutableArray<SemanticValidationRule> BindConceptValidations(ConceptSyntax concept)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            foreach (var validation in concept.Validations ?? [])
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' code validation requires a constrained implementation attachment.", validation.Location);
                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' requirement conditions are not admitted by the first ESM v1 vertical.", requirement.Location);
                }

                foreach (var rule in declarative.Rules)
                {
                    if (rule.Property != ValidationRuleSyntax.ConceptValue || rule.Rule != ValidationRuleKind.NotEmpty ||
                        rule.Value is not null || rule.File is not null || rule.Code is not null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept validation rule '{rule.Rule}' is not admitted by the first ESM v1 vertical.", rule.Location);
                        continue;
                    }

                    validations.Add(new(default, SemanticValidationRuleKind.NotEmpty, null, rule.Message));
                }
            }

            return validations.ToImmutable();
        }

        SemanticCompositeType BindType(TypeSyntax type)
        {
            if (type.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' description is authoring metadata.", type.Location);
            }

            if (type.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' file reference is realization provenance.", type.File.Location);
            }

            var owner = _types[type.Name].Address;
            var properties = type.Properties.Select(property => BindProperty(owner, property, false)).ToImmutableArray();
            return new(_types[type.Name].Id, type.Name, properties);
        }

        SemanticModule BindModule(ModuleSyntax module)
        {
            if (module.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Module '{module.Name}' description is authoring metadata.", module.Location);
            }

            foreach (var template in module.ScreenTemplates)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Dialog template '{template.Name}' is explicitly deferred from the backend ESM v1 profile.", template.Location);
            }

            foreach (var form in module.Forms ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Form '{form.Name}' is explicitly deferred from the backend ESM v1 profile.", form.Location);
            }

            foreach (var contribution in module.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var address = SemanticAddress.ForModule(_applicationIdentity, module.Name);
            var id = Resolve(address, module.Location);
            var features = module.Features.Select(feature => BindFeature(module.Name, [], feature)).ToImmutableArray();
            return new(id, module.Name, features);
        }

        SemanticFeature BindFeature(string module, ImmutableArray<string> parentPath, FeatureSyntax feature)
        {
            if (feature.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Feature '{feature.Name}' description is authoring metadata.", feature.Location);
            }

            foreach (var contribution in feature.Contributions ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Contribution to '{contribution.ContributionPoint}' is explicitly deferred from the backend ESM v1 profile.", contribution.Location);
            }

            var path = parentPath.Add(feature.Name);
            var address = SemanticAddress.ForFeature(_applicationIdentity, module, path);
            var id = Resolve(address, feature.Location);
            var nested = feature.Features.Select(value => BindFeature(module, path, value)).ToImmutableArray();
            var slices = feature.Slices.Select(value => BindSlice(module, path, value)).ToImmutableArray();
            return new(id, feature.Name, nested, slices);
        }

        SemanticSlice BindSlice(string module, ImmutableArray<string> featurePath, SliceSyntax slice)
        {
            if (slice.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' description is authoring metadata.", slice.Location);
            }

            if (slice.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Slice '{slice.Name}' file reference is realization provenance.", slice.File.Location);
            }

            var kind = slice.Type switch
            {
                SliceType.StateChange => SemanticSliceKind.StateChange,
                SliceType.StateView => SemanticSliceKind.StateView,
                _ => SemanticSliceKind.Unknown
            };
            if (kind == SemanticSliceKind.Unknown)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Slice '{slice.Name}' of type '{slice.Type}' is not admitted by ESM v1.", slice.Location);
            }

            var address = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
            var id = Resolve(address, slice.Location);
            var events = slice.Events.Select(value => _eventDeclarations[value]).ToArray();
            var commands = slice.Commands.Select(value => BindCommand(address, value, _events)).ToImmutableArray();
            var readModels = (slice.ReadModels ?? []).Select(value => _readModelDeclarations[value].Model).ToImmutableArray();
            var projections = slice.Projections.Select(value => BindProjection(address, value)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var queries = slice.Queries.Select(value => _queryDeclarations.GetValueOrDefault(value)).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var commandsByName = commands.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var specifications = slice.Specifications
                .Select(value => BindSpecification(address, value, commandsByName))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            ReportUnsupportedSliceMembers(slice);
            return new(id, slice.Name, kind, [.. events.Select(_ => _.Contract)], commands, readModels, projections, queries, specifications);
        }

        BoundEvent BindEvent(SemanticAddress slice, EventSyntax @event)
        {
            if (@event.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Event '{@event.Name}' file reference is realization provenance.", @event.File.Location);
            }

            if ((@event.Tags ?? []).Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event '{@event.Name}' tags are not admitted by ESM v1.", @event.Location);
            }

            var address = SemanticAddress.ForEventContract(slice, @event.Name);
            var semanticAssignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            var contractAssignment = documents.IdentityCatalog.ResolveEventContract(address);
            Map(semanticAssignment.Id, contractAssignment.Origin, @event.Location);
            var properties = @event.Properties.Select(property => BindProperty(address, property, false)).ToImmutableArray();
            return new(
                @event,
                new(
                    semanticAssignment.Id,
                    contractAssignment.Id,
                    contractAssignment.Revision,
                    @event.Name,
                    properties),
                properties.ToDictionary(_ => _.Name, StringComparer.Ordinal));
        }

        SemanticCommand BindCommand(
            SemanticAddress slice,
            CommandSyntax command,
            Dictionary<string, BoundEvent> events)
        {
            if (command.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Command '{command.Name}' description is authoring metadata.", command.Location);
            }

            if (command.Authorize is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' authorization requires portable policy semantics.", command.Authorize.Location);
            }

            foreach (var reads in command.Reads ?? [])
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' with legacy semantics that cannot imply decision consistency.",
                    reads.Location);
            }

            if (command.Concurrency is not null)
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.",
                    command.Concurrency.Location);
            }

            if (command.Handler is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' handler requires a constrained implementation attachment.", command.Handler.Location);
            }

            var address = SemanticAddress.ForCommand(slice, command.Name);
            var id = Resolve(address, command.Location);
            var properties = command.Properties.Select(property => BindProperty(address, property, property.IsIdentifier)).ToImmutableArray();
            var propertiesByName = properties.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var validations = BindValidations(command, propertiesByName);
            var produced = command.Produces
                .Select(value => BindProducedEvent(command, value, propertiesByName, events))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(id, command.Name, properties, validations, produced);
        }

        ImmutableArray<SemanticValidationRule> BindValidations(
            CommandSyntax command,
            Dictionary<string, SemanticProperty> properties)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            foreach (var validation in command.Validations)
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' code validation requires a constrained implementation attachment.", validation.Location);
                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' requirement conditions are not admitted by the first ESM v1 vertical.", requirement.Location);
                }

                foreach (var rule in declarative.Rules)
                {
                    if (rule.Rule != ValidationRuleKind.NotEmpty || rule.Value is not null || rule.File is not null || rule.Code is not null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule '{rule.Rule}' on '{rule.Property}' is not admitted by the first ESM v1 vertical.", rule.Location);
                        continue;
                    }

                    if (!properties.TryGetValue(rule.Property, out var property))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Validation rule property '{rule.Property}' is unresolved on command '{command.Name}'.", rule.Location);
                        continue;
                    }

                    validations.Add(new(property.Id, SemanticValidationRuleKind.NotEmpty, null, rule.Message));
                }
            }

            return validations.ToImmutable();
        }

        SemanticProducedEvent? BindProducedEvent(
            CommandSyntax command,
            ProducesSyntax produced,
            Dictionary<string, SemanticProperty> commandProperties,
            Dictionary<string, BoundEvent> events)
        {
            if (!events.TryGetValue(produced.Event, out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event '{produced.Event}' is not declared in slice '{command.Name}'.", produced.Location);
                return null;
            }

            if (produced.When is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Conditional production of '{produced.Event}' is not admitted by the first ESM v1 vertical.", produced.When.Location);
            }

            if ((produced.Tags ?? []).Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Produced event '{produced.Event}' tags are not admitted by ESM v1.", produced.Location);
            }

            var destination = produced.For is null
                ? null
                : BindPropertyExpression(produced.For, commandProperties, "produced event destination");
            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            foreach (var mapping in produced.Mappings)
            {
                if (!@event.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event mapping target '{mapping.Property}' is unresolved on '{@event.Syntax.Name}'.", mapping.Location);
                    continue;
                }

                if (BindExpression(mapping.Source, commandProperties, SemanticExpressionRootKind.Command, "produced event mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(@event.Contract.Id, null, destination, mappings.ToImmutable());
        }

        SemanticExpression? BindPropertyExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            string description)
        {
            if (expression is PathExpressionSyntax path && properties.TryGetValue(path.Path, out var property))
            {
                return SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id);
            }

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A {description} must resolve to one command property in ESM v1.", expression.Location);
            return null;
        }

        SemanticExpression? BindExpression(
            ExpressionSyntax expression,
            Dictionary<string, SemanticProperty> properties,
            SemanticExpressionRootKind root,
            string description) => expression switch
        {
            PathExpressionSyntax path when properties.TryGetValue(path.Path, out var property) =>
                SemanticExpression.Property(root, property.Id),
            LiteralExpressionSyntax literal => SemanticExpression.FromValue(BindLiteral(literal)),
            _ => UnsupportedExpression(expression, description)
        };

        SemanticValue BindLiteral(LiteralExpressionSyntax expression) => expression.Value switch
        {
            null => SemanticValue.Null,
            string value => SemanticValue.Text(value),
            bool value => SemanticValue.Boolean(value),
            double value => SemanticValue.Number(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            _ => throw new InvalidSemanticContract($"Literal value type '{expression.Value.GetType().Name}' is unsupported during semantic binding.")
        };

        SemanticExpression? UnsupportedExpression(ExpressionSyntax expression, string description)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} expression '{expression.GetType().Name}' is not admitted by ESM v1.", expression.Location);
            return null;
        }

        SemanticProjection? BindProjection(SemanticAddress slice, ProjectionSyntax projection)
        {
            if (projection.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Projection '{projection.Name}' file reference is realization provenance.", projection.File.Location);
            }

            if (projection.Sequence is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' sequence is not portable ESM v1 behavior.", projection.Location);
            }

            if (projection.ReadModel is null || !_readModels.TryGetValue(ShortName(projection.ReadModel), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{projection.Name}' read model is unresolved.", projection.Location);
                return null;
            }

            foreach (var block in projection.Blocks.Where(_ => _ is not FromSyntax))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection block '{block.GetType().Name}' is not admitted by the first ESM v1 vertical.", block.Location);
            }

            var address = SemanticAddress.ForProjection(slice, projection.Name);
            var id = Resolve(address, projection.Location);
            var transitions = projection.Blocks
                .OfType<FromSyntax>()
                .Select(value => BindProjectionTransition(projection, value, readModel))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(id, projection.Name, readModel.Model.Id, transitions);
        }

        SemanticProjectionTransition? BindProjectionTransition(
            ProjectionSyntax projection,
            FromSyntax from,
            BoundReadModel readModel)
        {
            var eventSpecs = from.Events.ToArray();
            if (eventSpecs.Length != 1 || !_events.TryGetValue(eventSpecs[0].Event, out var @event))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' transition must name one unambiguous event in the first ESM v1 vertical.", from.Location);
                return null;
            }

            if (from.ParentKey is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' parent keys are not admitted by the first ESM v1 vertical.", from.ParentKey.Location);
            }

            var keySyntax = eventSpecs[0].Key ?? ExpressionFrom(from.Key) ?? ExpressionFrom(projection.Key);
            if (keySyntax is null || BindExpression(keySyntax, @event.Properties, SemanticExpressionRootKind.Event, "projection affected key") is not { } key)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{projection.Name}' transition requires one resolved affected key.", from.Location);
                return null;
            }

            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            var explicitlyMapped = from.Mappings.Select(_ => _.Property).ToHashSet(StringComparer.Ordinal);
            if (projection.AutoMap != AutoMapMode.Disabled)
            {
                foreach (var property in readModel.Model.Properties.Where(_ => !explicitlyMapped.Contains(_.Name)))
                {
                    if (@event.Properties.TryGetValue(property.Name, out var source))
                    {
                        mappings.Add(new(property.Id, SemanticExpression.Property(SemanticExpressionRootKind.Event, source.Id)));
                    }
                }
            }

            foreach (var mapping in from.Mappings)
            {
                if (mapping is not SetMappingSyntax set || !readModel.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection mapping '{mapping.Property}' is not a direct set mapping admitted by ESM v1.", mapping.Location);
                    continue;
                }

                if (BindExpression(set.Source, @event.Properties, SemanticExpressionRootKind.Event, "projection mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(
                @event.Contract.Id,
                new(AffectedInstanceCardinality.One, key),
                mappings.ToImmutable());
        }

        ExpressionSyntax? ExpressionFrom(KeySyntax? key) => key switch
        {
            null => null,
            ExpressionKeySyntax expression => expression.Expression,
            _ => UnsupportedKey(key)
        };

        ExpressionSyntax? UnsupportedKey(KeySyntax key)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection key '{key.GetType().Name}' is not admitted by the first ESM v1 vertical.", key.Location);
            return null;
        }

        SemanticSpecification? BindSpecification(
            SemanticAddress slice,
            SpecificationSyntax specification,
            Dictionary<string, SemanticCommand> commands)
        {
            if (specification.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Specification '{specification.Name}' file reference is realization provenance.", specification.File.Location);
            }

            if (specification.When is null || !commands.TryGetValue(ShortName(specification.When.CommandType), out var command))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification '{specification.Name}' command is unresolved in its slice.", specification.Location);
                return null;
            }

            var address = SemanticAddress.ForSpecification(slice, specification.Name);
            var id = Resolve(address, specification.Location);
            var givenEvents = specification.Given.Select(BindSpecificationEvent).Where(_ => _ is not null).Select(_ => _!).ToImmutableArray();
            var givenReadModels = (specification.GivenReadModels ?? [])
                .Select(value => BindReadModelState(value.Name, value.Properties, value.Location))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var when = new SemanticSpecificationCommand(
                command.Id,
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

            return BindReadModelState(readModel, values, location);
        }

        SemanticSpecificationReadModel? BindReadModelState(
            BoundReadModel readModel,
            IEnumerable<PropertyMappingSyntax> values,
            SourceLocation location)
        {
            var bound = BindPropertyValues(values, readModel.Properties, "specification read model");
            var identifier = readModel.Model.Properties.SingleOrDefault(_ => _.IsIdentifier);
            var key = identifier is null ? null : bound.SingleOrDefault(_ => _.TargetProperty == identifier.Id)?.Value;
            if (key is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification read model '{readModel.Model.Name}' does not state its identifier property.", location);
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
                .Select(result => BindReadModelState(readModel, result.Properties, result.Location))
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

        void ReportUnsupportedSliceMembers(SliceSyntax slice)
        {
            foreach (var reducer in slice.Reducers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reducer '{reducer.Name}' requires a portable reducer contract.", reducer.Location);
            }

            foreach (var reaction in slice.Reactions)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' requires portable occurrence and effect semantics.", reaction.Location);
            }

            foreach (var capture in slice.Captures)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture.Name}' requires a portable compiled CDL plan.", capture.Location);
            }

            foreach (var constraint in slice.Constraints)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Constraint '{constraint.Name}' requires a portable constraint contract.", constraint.Location);
            }

            foreach (var screen in slice.Screens)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen '{screen.Name}' is explicitly deferred from the backend ESM v1 profile.", screen.Location);
            }
        }

        SemanticProperty BindProperty(SemanticAddress owner, PropertySyntax property, bool isIdentifier)
        {
            var address = SemanticAddress.ForProperty(owner, property.Name);
            var id = Resolve(address, property.Location);
            return new(id, property.Name, BindTypeReference(property.Type), isIdentifier);
        }

        SemanticTypeReference BindTypeReference(TypeRefSyntax type) => type.Name switch
        {
            "Uuid" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid, type.IsCollection, type.IsOptional),
            "String" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, type.IsCollection, type.IsOptional),
            "Int" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, type.IsCollection, type.IsOptional),
            "Decimal" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber, type.IsCollection, type.IsOptional),
            "Bool" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean, type.IsCollection, type.IsOptional),
            "Date" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Date, type.IsCollection, type.IsOptional),
            "DateTime" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime, type.IsCollection, type.IsOptional),
            _ when _concepts.TryGetValue(type.Name, out var concept) => SemanticTypeReference.ForConcept(concept.Id, type.IsCollection, type.IsOptional),
            _ when _types.TryGetValue(type.Name, out var composite) => SemanticTypeReference.ForCompositeType(composite.Id, type.IsCollection, type.IsOptional),
            _ => throw new InvalidSemanticContract($"Type reference '{type.Name}' is unresolved during semantic binding.")
        };

        string ShortName(string value) => value[(value.LastIndexOf('.') + 1)..];

        SemanticPrimitiveType Primitive(string value) => value switch
        {
            "Uuid" => SemanticPrimitiveType.Uuid,
            "String" => SemanticPrimitiveType.Text,
            "Int" => SemanticPrimitiveType.WholeNumber,
            "Decimal" => SemanticPrimitiveType.DecimalNumber,
            "Bool" => SemanticPrimitiveType.Boolean,
            "Date" => SemanticPrimitiveType.Date,
            "DateTime" => SemanticPrimitiveType.DateTime,
            _ => throw new InvalidSemanticContract($"Primitive type '{value}' is unsupported during semantic binding.")
        };

        SemanticId Resolve(SemanticAddress address, SourceLocation location)
        {
            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            Map(assignment.Id, assignment.Origin, location);
            return assignment.Id;
        }

        void Map(SemanticId id, SemanticIdentityOrigin origin, SourceLocation location)
        {
            if (DocumentAt(location) is not { } document)
            {
                return;
            }

            try
            {
                var offset = OffsetAt(document.Text, location);
                var span = SemanticSourceSpan.Create(document.Id, offset, 0, location.Line, location.Column, location.Line, location.Column);
                _sourceMapEntries.Add(new(id, span, origin));
            }
            catch (InvalidSemanticContract exception)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, location);
            }
        }

        SemanticSourceDocument? DocumentAt(SourceLocation location)
        {
            if (location.Path is null && documents.Documents.Length == 1)
            {
                return documents.Documents[0];
            }

            var document = documents.Documents.FirstOrDefault(value =>
                string.Equals(value.DisplayPath, location.Path, StringComparison.OrdinalIgnoreCase));
            if (document is null)
            {
                Error(
                    DiagnosticCodes.UnknownSemanticSourceDocument,
                    $"Source location path '{location.Path ?? "<none>"}' does not identify one supplied semantic document.",
                    location);
            }

            return document;
        }

        int OffsetAt(string text, SourceLocation location)
        {
            var line = 1;
            var offset = 0;
            while (line < location.Line && offset < text.Length)
            {
                if (text[offset] == '\r')
                {
                    offset++;
                    if (offset < text.Length && text[offset] == '\n')
                    {
                        offset++;
                    }

                    line++;
                }
                else if (text[offset++] == '\n')
                {
                    line++;
                }
            }

            if (line != location.Line)
            {
                throw new InvalidSemanticContract("A semantic syntax location is outside its source document.");
            }

            var lineStart = offset;
            while (offset < text.Length && text[offset] is not ('\r' or '\n'))
            {
                offset++;
            }

            var lineLength = offset - lineStart;
            if (location.Column > lineLength + 1)
            {
                throw new InvalidSemanticContract("A semantic syntax column is outside its source line.");
            }

            return lineStart + location.Column - 1;
        }

        sealed record BoundEvent(
            EventSyntax Syntax,
            SemanticEventContract Contract,
            Dictionary<string, SemanticProperty> Properties);

        sealed record BoundReadModel(
            ReadModelSyntax Syntax,
            SemanticReadModel Model,
            Dictionary<string, SemanticProperty> Properties);
    }
}
