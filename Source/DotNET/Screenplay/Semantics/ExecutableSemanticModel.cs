// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the immutable, versioned executable semantic model.
/// </summary>
public sealed record ExecutableSemanticModel
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ExecutableSemanticModel, CachedDeprecations> _deprecations = [];

    ExecutableSemanticModel(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticRevision revision,
        SemanticApplication application)
    {
        LanguageVersion = languageVersion;
        SemanticVersion = semanticVersion;
        Revision = revision;
        Application = application;
    }

    /// <summary>
    /// Gets the source language version.
    /// </summary>
    public LanguageVersion LanguageVersion { get; }

    /// <summary>
    /// Gets the portable execution semantic version.
    /// </summary>
    public SemanticVersion SemanticVersion { get; }

    /// <summary>
    /// Gets the deterministic revision over canonical semantic content, excluding this revision field.
    /// </summary>
    public SemanticRevision Revision { get; }

    /// <summary>
    /// Gets the semantic application graph.
    /// </summary>
    public SemanticApplication Application { get; }

    /// <summary>
    /// Gets deprecation warnings for legacy projection transition cardinalities. ESM has no source locations;
    /// these model-level diagnostics point at the beginning of the document.
    /// </summary>
    /// <remarks>Only flat projection transitions carry these cardinalities. Query cardinality is unaffected.</remarks>
    public ImmutableArray<Diagnostic> DeprecationDiagnostics => _deprecations.GetValue(this, static model => new CachedDeprecations(
    [
        .. model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Projections)
            .SelectMany(projection => projection.Transitions
                .Where(transition => transition.AffectedInstance.Cardinality is AffectedInstanceCardinality.ZeroOrOne or AffectedInstanceCardinality.Many)
                .Select(transition => Diagnostic.Warning(
                    DiagnosticCodes.DeprecatedProjectionTransitionCardinality,
                    $"Projection '{projection.Name}' event contract '{transition.EventContract}' uses deprecated affected-instance cardinality '{transition.AffectedInstance.Cardinality}' on a transition; Chronicle routes one key per transition. Use a join for many affected instances.",
                    SourceLocation.Start)))
    ])).Values;

    /// <summary>
    /// Creates a validated model and computes its deterministic revision.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable execution semantic version.</param>
    /// <param name="application">The application graph.</param>
    /// <returns>The validated executable semantic model.</returns>
    /// <exception cref="InvalidSemanticContract">The model is malformed or contains unresolved references.</exception>
    public static ExecutableSemanticModel Create(
        LanguageVersion languageVersion,
        SemanticVersion semanticVersion,
        SemanticApplication application)
    {
        EsmSchemaV10Support.EnsureSupported(languageVersion, semanticVersion);
        SemanticModelValidator.Validate(application, semanticVersion);
        var withoutRevision = SemanticModelCanonicalJson.SerializeWithoutRevision(languageVersion, semanticVersion, application);
        var revision = SemanticRevision.Compute(withoutRevision);
        return new(languageVersion, semanticVersion, revision, application);
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    sealed record CachedDeprecations(ImmutableArray<Diagnostic> Values);
}

internal static partial class SemanticModelValidator
{
    public static void Validate(SemanticApplication application, SemanticVersion semanticVersion = default)
    {
        if (application is null)
        {
            throw new InvalidSemanticContract("The semantic application cannot be null.");
        }

        var context = new ValidationContext(semanticVersion == default ? SemanticVersion.V1 : semanticVersion, application);
        context.RegisterApplication(application);
        ValidateEventRoutesVersion(application, semanticVersion);
        ValidatePublicEventsVersion(application, semanticVersion);
        ValidateReactionIdentityVersion(application, semanticVersion);
        context.ValidateReferences(application);
        ValidateEventSourceVersion(application, semanticVersion);
        ValidateEventLineageVersion(application, semanticVersion);
        ValidateReadModelAbsenceVersion(application, semanticVersion);
        ValidateAutomationVersion(application, semanticVersion);
        ValidateResponseVersion(application, semanticVersion);
        ValidateImplementationAttachmentVersion(application, semanticVersion);
    }

    internal static SemanticTypeReference DeclaredEventSourceType(IEnumerable<SemanticCommand> commands, SemanticId eventContract) =>
        DeclaredEventSourceType(commands, [], [], eventContract);

    // Since ESM v6 a reaction or a capture can append an event too: a reaction to the event source its 'for' names, or
    // to that of the event that set it off, and a capture to the one its record key names.
    internal static SemanticTypeReference DeclaredEventSourceType(
        IEnumerable<SemanticCommand> commands,
        IEnumerable<SemanticReaction> reactions,
        IEnumerable<SemanticCapture> captures,
        SemanticId eventContract)
    {
        var producerTypes = ProducerTypes([.. commands], [.. reactions], [.. captures], eventContract, [])
            .Distinct().ToArray();
        if (producerTypes.Length != 1)
        {
            throw new InvalidSemanticContract("A specification event source needs one unambiguous declared producer destination type.");
        }

        return producerTypes[0];
    }

    internal static SemanticTypeReference? ProducedEventSourceType(SemanticCommand command, SemanticProducedEvent produced) =>
        (produced.Route is not null || command.Properties.Any(property => property.IsGenerated && property.IsIdentifier)) &&
        produced.Destination is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } route
            ? command.Properties.SingleOrDefault(property => property.Id == route.Target)?.Type
            : command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type;

    static List<SemanticTypeReference> ProducerTypes(
        SemanticCommand[] commands,
        SemanticReaction[] reactions,
        SemanticCapture[] captures,
        SemanticId eventContract,
        HashSet<SemanticId> visited)
    {
        var types = commands.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => ProducedEventSourceType(command, produced)))
            .OfType<SemanticTypeReference>().ToList();
        visited.Add(eventContract);
        foreach (var trigger in reactions.SelectMany(reaction => reaction.Triggers))
        {
            foreach (var produced in trigger.Produces.Where(produced => produced.EventContract == eventContract))
            {
                if (produced.DestinationType is { } type)
                {
                    types.Add(type);
                }
                else if (trigger.Kind == SemanticReactionTriggerKind.Event && !visited.Contains(trigger.Source))
                {
                    types.AddRange(ProducerTypes(commands, reactions, captures, trigger.Source, visited));
                }
            }
        }

        types.AddRange(captures.SelectMany(CaptureAppends)
            .Where(append => append.EventContract == eventContract)
            .Select(append => append.EventSourceType));
        return types;
    }

    static IEnumerable<SemanticCaptureAppend> CaptureAppends(SemanticCapture capture) =>
        capture.Appends
            .Concat(capture.Children.SelectMany(children => children.Appends))
            .Concat(capture.Nested.SelectMany(nested => nested.Appends));

    static void RejectDuplicateRequirementIds(IEnumerable<string?> requirementIds, string owner)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requirementId in requirementIds.Where(id => id is not null))
        {
            if (!seen.Add(requirementId!))
            {
                throw new InvalidSemanticContract($"{owner} has duplicate validation requirement identities.");
            }
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    private sealed partial class ValidationContext
    {
        readonly HashSet<SemanticId> _ids = [];
        readonly Dictionary<SemanticId, SemanticConcept> _concepts = [];
        readonly Dictionary<SemanticId, SemanticCompositeType> _types = [];
        readonly Dictionary<SemanticId, SemanticEventContract> _events = [];
        readonly HashSet<EventContractId> _eventContractIds = [];
        readonly Dictionary<SemanticId, SemanticCommand> _commands = [];
        readonly Dictionary<SemanticId, SemanticReadModel> _readModels = [];
        readonly Dictionary<SemanticId, SemanticKeyedQuery> _queries = [];
        readonly SemanticValueValidator _valueValidator;

        readonly SemanticVersion _semanticVersion;

        public ValidationContext(SemanticVersion semanticVersion, SemanticApplication application)
        {
            _eventRoutesApplication = application;
            _semanticVersion = semanticVersion;
            _valueValidator = new(_concepts, _types);
        }

        public void RegisterApplication(SemanticApplication application)
        {
            Register(application.Id, application.Name, "application");
            RequireObjects(application.Concepts, nameof(application.Concepts), "concept");
            RequireObjects(application.Types, nameof(application.Types), "composite type");
            RequireObjects(application.Modules, nameof(application.Modules), "module");
            RejectDuplicateNames(application.Concepts.Select(_ => _.Name), "concept");
            RejectDuplicateNames(application.Types.Select(_ => _.Name), "composite type");
            RejectDuplicateNames(application.Modules.Select(_ => _.Name), "module");

            foreach (var concept in application.Concepts)
            {
                RegisterConcept(concept);
            }

            foreach (var type in application.Types)
            {
                RegisterType(type);
            }

            RegisterEventSources(application);
            RegisterTriggers(application);
            foreach (var module in application.Modules)
            {
                RegisterModule(module);
            }
        }

        public void ValidateReferences(SemanticApplication application)
        {
            foreach (var concept in application.Concepts)
            {
                ValidatePrimitive(concept.Primitive);
                if (!concept.Values.IsEmpty && concept.Primitive != SemanticPrimitiveType.Text)
                {
                    throw new InvalidSemanticContract($"Enumeration concept '{concept.Name}' must use the text primitive representation.");
                }

                var conceptType = SemanticTypeReference.ForConcept(concept.Id);
                foreach (var validation in concept.Validations)
                {
                    ValidateValidation(validation, true, conceptType);
                }

                RejectDuplicateRequirementIds(concept.Validations.Select(validation => validation.RequirementId), $"Concept '{concept.Name}'");
            }

            foreach (var type in application.Types)
            {
                ValidateProperties(type.Properties);
            }

            ValidateEventSources();
            ValidateTriggers();
            foreach (var slice in AllSlices(application))
            {
                ValidateSlice(slice);
            }

            ValidatePolicies(application);
        }

        void RegisterConcept(SemanticConcept concept)
        {
            Register(concept.Id, concept.Name, "concept");
            RequireArray(concept.Values, nameof(concept.Values));
            RequireObjects(concept.Validations, nameof(concept.Validations), "validation rule");
            RejectDuplicateNames(concept.Values, $"value on concept '{concept.Name}'");
            _concepts.Add(concept.Id, concept);
        }

        void RegisterType(SemanticCompositeType type)
        {
            Register(type.Id, type.Name, "composite type");
            RegisterProperties(type.Properties, $"composite type '{type.Name}'");
            _types.Add(type.Id, type);
        }

        void RegisterModule(SemanticModule module)
        {
            Register(module.Id, module.Name, "module");
            RequireObjects(module.Features, nameof(module.Features), "feature");
            RejectDuplicateNames(module.Features.Select(_ => _.Name), $"feature in module '{module.Name}'");
            foreach (var feature in module.Features)
            {
                RegisterFeature(feature);
            }
        }

        void RegisterFeature(SemanticFeature feature)
        {
            Register(feature.Id, feature.Name, "feature");
            RequireObjects(feature.Features, nameof(feature.Features), "nested feature");
            RequireObjects(feature.Slices, nameof(feature.Slices), "slice");
            RejectDuplicateNames(feature.Features.Select(_ => _.Name), $"nested feature in '{feature.Name}'");
            RejectDuplicateNames(feature.Slices.Select(_ => _.Name), $"slice in feature '{feature.Name}'");
            foreach (var nested in feature.Features)
            {
                RegisterFeature(nested);
            }

            foreach (var slice in feature.Slices)
            {
                RegisterSlice(slice);
            }
        }

        void RegisterSlice(SemanticSlice slice)
        {
            Register(slice.Id, slice.Name, "slice");
            RequireObjects(slice.Events, nameof(slice.Events), "event contract");
            RequireObjects(slice.Commands, nameof(slice.Commands), "command");
            RequireObjects(slice.ReadModels, nameof(slice.ReadModels), "read model");
            RequireObjects(slice.Projections, nameof(slice.Projections), "projection");
            RequireObjects(slice.Reducers, nameof(slice.Reducers), "reducer");
            RejectDuplicateNames(slice.Reducers.Select(_ => _.Name), $"reducer in slice '{slice.Name}'");
            RequireObjects(slice.Queries, nameof(slice.Queries), "query");
            RequireObjects(slice.Specifications, nameof(slice.Specifications), "specification");
            RejectDuplicateNames(slice.Events.Select(_ => _.Name), $"event in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Commands.Select(_ => _.Name), $"command in slice '{slice.Name}'");
            RejectDuplicateNames(slice.ReadModels.Select(_ => _.Name), $"read model in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Projections.Select(_ => _.Name), $"projection in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Queries.Select(_ => _.Name), $"query in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Specifications.Select(_ => _.Name), $"specification in slice '{slice.Name}'");

            foreach (var eventContract in slice.Events)
            {
                RegisterEvent(eventContract);
            }

            foreach (var command in slice.Commands)
            {
                RegisterCommand(command);
            }

            foreach (var readModel in slice.ReadModels)
            {
                RegisterReadModel(readModel);
            }

            foreach (var projection in slice.Projections)
            {
                Register(projection.Id, projection.Name, "projection");
                RequireObjects(projection.Transitions, nameof(projection.Transitions), "projection transition");
            }

            foreach (var query in slice.Queries)
            {
                Register(query.Id, query.Name, "query");
                if (query.Argument is not null)
                {
                    Register(query.Argument.Id, query.Argument.Name, "query argument");
                }

                _queries.Add(query.Id, query);
            }

            foreach (var specification in slice.Specifications)
            {
                Register(specification.Id, specification.Name, "specification");
                RequireSpecificationArrays(specification);
            }

            RegisterConstraints(slice);
            RegisterAutomation(slice);
        }

        void RegisterEvent(SemanticEventContract eventContract)
        {
            Register(eventContract.Id, eventContract.Name, "event contract");
            if (!eventContract.ContractId.IsSet || !eventContract.Revision.IsValid || eventContract.Revision.Value == uint.MaxValue)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has an invalid contract revision.");
            }

            if (eventContract.PriorRevisions.IsDefault || eventContract.PriorRevisions.Length != eventContract.Revision.Value - 1 ||
                eventContract.Predecessor != (eventContract.Revision == EventContractRevision.Initial ? null : new EventContractRevision(eventContract.Revision.Value - 1)))
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has incomplete predecessor lineage.");
            }

            for (var index = 0; index < eventContract.PriorRevisions.Length; index++)
            {
                var prior = eventContract.PriorRevisions[index];
                if (prior is null || prior.Revision.Value != (uint)index + 1 ||
                    prior.Predecessor != (index == 0 ? null : new EventContractRevision((uint)index)) || prior.Properties.IsDefault)
                {
                    throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' has invalid historical revision lineage.");
                }

                ValidateTags(prior.Tags);
                RegisterProperties(prior.Properties, $"event contract '{eventContract.Name}' revision {prior.Revision.Value}");
            }

            if (!_eventContractIds.Add(eventContract.ContractId))
            {
                throw new InvalidSemanticContract($"Event contract identity '{eventContract.ContractId}' is duplicated.");
            }

            ValidateTags(eventContract.Tags);
            _events.Add(eventContract.Id, eventContract);
            RegisterProperties(eventContract.Properties, $"event contract '{eventContract.Name}'");
        }

        void RegisterCommand(SemanticCommand command)
        {
            Register(command.Id, command.Name, "command");
            RegisterProperties(command.Properties, $"command '{command.Name}'");
            RequireObjects(command.Validations, nameof(command.Validations), "validation rule");
            RequireObjects(command.Produces, nameof(command.Produces), "produced event");
            _commands.Add(command.Id, command);
        }

        void RegisterReadModel(SemanticReadModel readModel)
        {
            Register(readModel.Id, readModel.Name, "read model");
            RegisterProperties(readModel.Properties, $"read model '{readModel.Name}'");
            _readModels.Add(readModel.Id, readModel);
        }

        void RegisterProperties(ImmutableArray<SemanticProperty> properties, string owner)
        {
            RequireObjects(properties, nameof(properties), "property");
            RejectDuplicateNames(properties.Select(_ => _.Name), $"property on {owner}");
            foreach (var property in properties)
            {
                Register(property.Id, property.Name, "property");
            }
        }

        void ValidateSlice(SemanticSlice slice)
        {
            ValidateEnum(slice.Kind, SemanticSliceKind.Unknown, "slice kind");
            ValidatePublicEventSlice(slice);
            foreach (var eventContract in slice.Events)
            {
                ValidateProperties(eventContract.Properties);
                foreach (var prior in eventContract.PriorRevisions)
                {
                    ValidateProperties(prior.Properties);
                }
            }

            foreach (var command in slice.Commands)
            {
                ValidateCommand(command);
            }

            foreach (var readModel in slice.ReadModels)
            {
                ValidateProperties(readModel.Properties);
                IdentifierProperty(readModel);
            }

            foreach (var projection in slice.Projections)
            {
                ValidateProjection(projection);
            }

            foreach (var reducer in slice.Reducers)
            {
                ValidateReducer(reducer);
            }

            foreach (var query in slice.Queries)
            {
                ValidateQuery(query);
            }

            foreach (var specification in slice.Specifications)
            {
                ValidateSpecification(specification);
            }

            ValidateConstraints(slice);
            ValidateAutomation(slice);
        }

        void ValidateCommandDestination(SemanticCommand command)
        {
            if (command.Destination is null)
            {
                return;
            }

            if (_semanticVersion == SemanticVersion.V1)
            {
                throw new InvalidSemanticContract("A command state-change destination requires ESM v2.");
            }

            var identity = command.Destination.Value is SemanticResolvedExpression resolved
                ? command.Properties.SingleOrDefault(property => property.Id == resolved.Target && resolved.Root == SemanticExpressionRootKind.Command)
                : null;
            if (identity is not { IsIdentifier: true, Type.IsCollection: false, Type.IsOptional: false } ||
                identity.Type != command.Destination.Type)
            {
                throw new InvalidSemanticContract("A command destination must reference a required scalar command identifier with the same type.");
            }
        }

        void ValidateCommand(SemanticCommand command)
        {
            ValidateCommandDestination(command);
            ValidateProperties(command.Properties, true);
            ValidateResponse(command);
            ValidateCommandRoute(command);
            var properties = Properties([.. command.Properties.Where(property => !property.IsGenerated)]);
            ValidateCommandRequirements(command, properties);
            if (command.CodeValidations.IsDefault || command.CodeValidations.Any(block => block is null || string.IsNullOrWhiteSpace(block.RequirementId)) ||
                (command.CodeValidations.Length > 0 && !_semanticVersion.IsAtLeast(SemanticVersion.V3)))
            {
                throw new InvalidSemanticContract("Command code validation requires ESM v3 and a requirement identity.");
            }

            foreach (var validation in command.Validations)
            {
                if (!properties.TryGetValue(validation.Property, out var property))
                {
                    throw new InvalidSemanticContract($"Validation property '{validation.Property}' is unresolved.");
                }

                ValidateValidation(validation, false, property.Type);
            }

            RejectDuplicateRequirementIds(
                command.CodeValidations.Select(block => block.RequirementId).Concat(command.Validations.Select(validation => validation.RequirementId)),
                $"Command '{command.Name}'");

            foreach (var produced in command.Produces)
            {
                ValidateProducedEvent(produced, command);
            }
        }

        void ValidateCommandRequirements(SemanticCommand command, Dictionary<SemanticId, SemanticProperty> properties)
        {
            if (command.Requirements.IsDefault) throw new InvalidSemanticContract("Command requirements cannot be default.");
            foreach (var requirement in command.Requirements)
            {
                if (requirement is null || requirement.Message?.Length == 0) throw new InvalidSemanticContract("A requirement or its message is invalid.");
                ValidateEnum(requirement.Severity, (SemanticValidationSeverity)(-1), "requirement severity");
                ValidateCondition(requirement.Condition, properties);
            }
        }

        void ValidateProperties(ImmutableArray<SemanticProperty> properties, bool command = false)
        {
            foreach (var property in properties)
            {
                ValidateTypeReference(property.Type);
                if (property.IsGenerated && (!command || !_semanticVersion.IsAtLeast(SemanticVersion.V7) ||
                    property.Type is not { Kind: SemanticTypeReferenceKind.Concept, IsCollection: false, IsOptional: false } ||
                    !_concepts.TryGetValue(property.Type.Target, out var concept) || concept.Primitive != SemanticPrimitiveType.Uuid ||
                    !concept.Validations.IsEmpty))
                {
                    throw new InvalidSemanticContract("A generated property requires an ESM v7 command and a required scalar UUID-backed concept without validation rules.");
                }
            }
        }

        void ValidateTypeReference(SemanticTypeReference type)
        {
            RejectNull(type, "type reference");
            ValidateEnum(type.Kind, SemanticTypeReferenceKind.Unknown, "type reference kind");
            switch (type.Kind)
            {
                case SemanticTypeReferenceKind.Primitive when Enum.IsDefined(type.Primitive) && type.Primitive != SemanticPrimitiveType.Unknown && !type.Target.IsSet:
                    return;
                case SemanticTypeReferenceKind.Concept when type.Primitive == SemanticPrimitiveType.Unknown && type.Target.IsSet && _concepts.ContainsKey(type.Target):
                    return;
                case SemanticTypeReferenceKind.CompositeType when type.Primitive == SemanticPrimitiveType.Unknown && type.Target.IsSet && _types.ContainsKey(type.Target):
                    return;
                default:
                    throw new InvalidSemanticContract("A semantic type reference is malformed or unresolved.");
            }
        }

        bool IsEnumeration(SemanticTypeReference type) =>
            type.Kind == SemanticTypeReferenceKind.Concept &&
            _concepts.TryGetValue(type.Target, out var concept) &&
            !concept.Values.IsEmpty;

        void ValidateProducedEvent(SemanticProducedEvent produced, SemanticCommand command)
        {
            if (produced.DestinationType is not null)
            {
                throw new InvalidSemanticContract("A command production cannot declare a reaction destination type.");
            }

            if (!_events.TryGetValue(produced.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Produced event contract '{produced.EventContract}' is unresolved.");
            }

            var sources = Properties(command.Properties);
            ValidateTags(produced.Tags);
            if (produced.When is not null) ValidateCondition(produced.When, Properties(command.Properties));
            if (produced.Condition is not null)
            {
                var conditionType = ResolveExpression(produced.Condition, SemanticExpressionRootKind.Command, sources);
                RequireBoolean(conditionType, "produced event condition");
            }

            if (produced.Destination is not null)
            {
                ValidateProducedDestination(produced, command, sources);
            }

            ValidateMappings(produced.Mappings, Properties(eventContract.Properties), SemanticExpressionRootKind.Command, sources);
            var mapped = produced.Mappings.Select(_ => _.TargetProperty).ToHashSet();
            if (eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateProducedDestination(SemanticProducedEvent produced, SemanticCommand command, Dictionary<SemanticId, SemanticProperty> sources)
        {
            var destinationType = ResolveExpression(produced.Destination!, SemanticExpressionRootKind.Command, sources);
            var destinationProperty = produced.Destination is SemanticResolvedExpression
            {
                Source: SemanticExpressionSourceKind.Property
            } resolved && sources.TryGetValue(resolved.Target, out var property)
                ? property
                : null;
            var generatedIdentifier = _semanticVersion.IsAtLeast(SemanticVersion.V7) && command.Properties.Any(value => value.IsGenerated && value.IsIdentifier);
            if (destinationType?.IsCollection is not false || destinationType.IsOptional || destinationProperty is null ||
                (!destinationProperty.IsIdentifier && !generatedIdentifier && produced.Route is null))
            {
                throw new InvalidSemanticContract("A produced event destination must resolve to one required scalar command identity.");
            }
        }

        void ValidateProjection(SemanticProjection projection)
        {
            if (projection.Target == SemanticProjectionTargetKind.Event)
            {
                ValidateEventTargetProjection(projection);
                return;
            }

            if (projection.Target != SemanticProjectionTargetKind.ReadModel)
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' has an unknown target kind.");
            }

            if (!_readModels.TryGetValue(projection.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Projection read model '{projection.ReadModel}' is unresolved.");
            }

            if (projection.Scope is not null)
            {
                ValidateProjectionScope(projection, readModel);
                return;
            }

            var targets = Properties(readModel.Properties);
            var identifier = IdentifierProperty(readModel);
            foreach (var transition in projection.Transitions)
            {
                ValidateReadModelTransition(transition, targets, identifier);
            }
        }

        void ValidateReadModelTransition(SemanticProjectionTransition transition, Dictionary<SemanticId, SemanticProperty> targets, SemanticProperty identifier)
        {
            if (!_events.TryGetValue(transition.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Projection event contract '{transition.EventContract}' is unresolved.");
            }

            RejectNull(transition.AffectedInstance, "affected instance");
            ValidateEnum(transition.AffectedInstance.Cardinality, AffectedInstanceCardinality.Unknown, "affected instance cardinality");
            if (_semanticVersion.IsAtLeast(SemanticVersion.V4) && transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One)
            {
                throw new InvalidSemanticContract("ESM v4 does not admit zeroOrOne or many projection transition cardinality.");
            }
            var sources = Properties(eventContract.Properties);
            var keyType = ResolveExpression(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, sources) ??
                throw new InvalidSemanticContract("An affected-instance key cannot be null.");

            var expectsCollection = transition.AffectedInstance.Cardinality == AffectedInstanceCardinality.Many;
            var expectsOptional = transition.AffectedInstance.Cardinality == AffectedInstanceCardinality.ZeroOrOne;
            if (keyType.IsCollection != expectsCollection || keyType.IsOptional != expectsOptional || !SameValueType(keyType, identifier.Type))
            {
                throw new InvalidSemanticContract("An affected-instance key type, optionality or cardinality is incompatible with the read model identifier.");
            }

            ValidateMappings(transition.Mappings, targets, SemanticExpressionRootKind.Event, sources);
        }

        void ValidateReducer(SemanticReducer reducer)
        {
            ValidateObserverFilter(reducer.From);
            var targetsEvent = reducer.Target == SemanticProjectionTargetKind.Event;
            if (reducer.Target is not (SemanticProjectionTargetKind.ReadModel or SemanticProjectionTargetKind.Event))
            {
                throw new InvalidSemanticContract($"Reducer '{reducer.Name}' has an unknown target kind.");
            }

            if (targetsEvent)
            {
                RequirePublicEventTarget(reducer.ReadModel, $"Reducer '{reducer.Name}'");
            }

            if (!_semanticVersion.IsAtLeast(SemanticVersion.V3) || string.IsNullOrWhiteSpace(reducer.Name) ||
                (!targetsEvent && !_readModels.ContainsKey(reducer.ReadModel)) || reducer.Transitions.IsDefaultOrEmpty)
            {
                throw new InvalidSemanticContract($"Reducer '{reducer.Name}' requires ESM v3, a read model, and transitions.");
            }

            var events = new HashSet<SemanticId>();
            var requirements = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transition in reducer.Transitions)
            {
                if (transition is null || !_events.ContainsKey(transition.EventContract) ||
                    string.IsNullOrWhiteSpace(transition.RequirementId) ||
                    !events.Add(transition.EventContract) || !requirements.Add(transition.RequirementId))
                {
                    throw new InvalidSemanticContract($"Reducer '{reducer.Name}' has an unresolved or duplicate transition.");
                }
            }
        }

        void ValidateQuery(SemanticKeyedQuery query)
        {
            if (!_readModels.TryGetValue(query.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Query read model '{query.ReadModel}' is unresolved.");
            }

            ValidateEnum(query.Cardinality, SemanticQueryCardinality.Unknown, "query cardinality");
            ValidateEnum(query.Delivery, SemanticQueryDelivery.Unknown, "query delivery");
            if ((query.Argument is null) != (query.KeyProperty is null))
            {
                throw new InvalidSemanticContract("A keyed query must declare both an argument and a key property; an unkeyed query declares neither.");
            }

            if (query.Argument is null)
            {
                if (query.Cardinality != SemanticQueryCardinality.Many)
                {
                    throw new InvalidSemanticContract("An unkeyed query must return a collection.");
                }

                return;
            }

            ValidateTypeReference(query.Argument.Type);
            if (!Properties(readModel.Properties).TryGetValue(query.KeyProperty!.Value, out var keyProperty))
            {
                throw new InvalidSemanticContract($"Query key property '{query.KeyProperty}' is unresolved.");
            }

            if (query.Argument.Type.IsCollection || keyProperty.Type.IsCollection || !SameType(query.Argument.Type, keyProperty.Type))
            {
                throw new InvalidSemanticContract("A query argument and key property must have the same scalar type and optionality.");
            }

            if (query.Cardinality == SemanticQueryCardinality.ZeroOrOne && !keyProperty.IsIdentifier)
            {
                throw new InvalidSemanticContract("An optional lookup query key property must identify one read-model instance.");
            }
        }

        void ValidateSpecificationEvent(SemanticSpecificationEvent value, bool then = false)
        {
            if (value.EventSource is not null && _semanticVersion == SemanticVersion.V1)
            {
                throw new InvalidSemanticContract("A specification event source requires ESM v2.");
            }

            if (!_events.TryGetValue(value.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Specification event '{value.EventContract}' is unresolved.");
            }

            ValidatePropertyValues(value.Values, eventContract.Properties, true);
            ValidateFixtureRoute(value.Route, value.Unrouted, then, value.EventSource, value.EventContract);
            if (value.EventSource is not null)
            {
                ValidateEventSource(value.EventSource, FixtureEventSourceType(value.Route, value.EventContract));
            }
        }

        void ValidateEventSource(SemanticEventSourceIdentity source, SemanticTypeReference? requiredType)
        {
            if (source.Type.IsCollection || source.Type.IsOptional || (requiredType is not null && source.Type != requiredType))
            {
                throw new InvalidSemanticContract("A specification event source must have the required scalar destination type.");
            }

            ValidateValue(source.Value, source.Type, "specification event source");
        }

        void ValidateSpecificationReadModel(SemanticSpecificationReadModel state, bool requireExact, bool requireIdentifier)
        {
            if (!_readModels.TryGetValue(state.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Specification read model '{state.ReadModel}' is unresolved.");
            }

            var identifier = IdentifierProperty(readModel);
            ValidateValue(state.Key, identifier.Type, "specification read model key");
            ValidatePropertyValues(state.Values, readModel.Properties, requireExact);
            var identifierValue = state.Values.SingleOrDefault(_ => _.TargetProperty == identifier.Id)?.Value;
            if ((requireIdentifier && identifierValue is null) ||
                (identifierValue is not null && !SemanticValueRules.AreEqual(state.Key, identifierValue)))
            {
                throw new InvalidSemanticContract("A specification read model key must equal its identifier property value.");
            }
        }

        void ValidateSpecificationQuery(SemanticSpecificationQueryResult result)
        {
            if (!_queries.TryGetValue(result.Query, out var query))
            {
                throw new InvalidSemanticContract($"Specification query '{result.Query}' is unresolved.");
            }

            if (query.Argument is not null) ValidateValue(result.Key, query.Argument.Type, "specification query key");
            RequireObjects(result.Results, nameof(result.Results), "specification query result state");
            var validCount = query.Cardinality switch
            {
                SemanticQueryCardinality.One => result.Results.Length == 1,
                SemanticQueryCardinality.ZeroOrOne => result.Results.Length <= 1,
                SemanticQueryCardinality.Many => true,
                _ => false
            };
            if (!validCount)
            {
                throw new InvalidSemanticContract("A specification query result count does not match its cardinality.");
            }

            var keyProperty = query.KeyProperty is { } queryKeyProperty
                ? _readModels[query.ReadModel].Properties.Single(_ => _.Id == queryKeyProperty)
                : null;
            foreach (var state in result.Results)
            {
                if (state.ReadModel != query.ReadModel)
                {
                    throw new InvalidSemanticContract("A specification query result uses the wrong read model or key.");
                }

                ValidateSpecificationReadModel(state, false, false);
                if (keyProperty is null)
                {
                    continue;
                }

                var queryKeyValue = state.Values.SingleOrDefault(_ => _.TargetProperty == keyProperty.Id)?.Value;
                if (queryKeyValue is null && keyProperty.IsIdentifier)
                {
                    queryKeyValue = state.Key;
                }

                if (queryKeyValue is null || !SemanticValueRules.AreEqual(queryKeyValue, result.Key) ||
                    (keyProperty.IsIdentifier && !SemanticValueRules.AreEqual(state.Key, result.Key)))
                {
                    throw new InvalidSemanticContract("A specification query result uses the wrong read model or key.");
                }
            }

            RejectDuplicateReadModelStates(result.Results, "query result read model");
        }

        void ValidateSpecificationError(SemanticSpecificationError error)
        {
            var isBare = error.Code is null && error.Message is null;
            var isMessageOnly = error.Code is null && !string.IsNullOrEmpty(error.Message);
            var isDetailed = !string.IsNullOrEmpty(error.Code) && !string.IsNullOrEmpty(error.Message);
            if (!isBare && !isMessageOnly && !isDetailed)
            {
                throw new InvalidSemanticContract("A specification rejection must be bare, message-only, or contain both a non-empty code and message.");
            }
        }

        void ValidateMappings(
            ImmutableArray<SemanticPropertyMapping> mappings,
            Dictionary<SemanticId, SemanticProperty> targets,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> sources)
        {
            RequireObjects(mappings, nameof(mappings), "property mapping");
            var mapped = new HashSet<SemanticId>();
            foreach (var mapping in mappings)
            {
                if (!mapped.Add(mapping.TargetProperty))
                {
                    throw new InvalidSemanticContract($"Mapping target property '{mapping.TargetProperty}' is duplicated.");
                }

                if (!targets.TryGetValue(mapping.TargetProperty, out var target))
                {
                    throw new InvalidSemanticContract($"Mapping target property '{mapping.TargetProperty}' is unresolved.");
                }

                if (mapping.Source is SemanticValueExpression { Kind: SemanticExpressionKind.Value } value)
                {
                    ValidateValue(value.Value, target.Type, "property mapping");
                }
                else
                {
                    var sourceType = ResolveExpression(mapping.Source, root, sources);
                    RequireCompatible(sourceType, target.Type, "property mapping");
                }
            }
        }

        void ValidatePropertyValues(
            ImmutableArray<SemanticPropertyValue> values,
            ImmutableArray<SemanticProperty> targetProperties,
            bool requireExact)
        {
            RequireObjects(values, nameof(values), "property value");
            var targets = Properties(targetProperties);
            var assigned = new HashSet<SemanticId>();
            foreach (var value in values)
            {
                RejectNull(value.Value, "semantic value");
                if (!assigned.Add(value.TargetProperty))
                {
                    throw new InvalidSemanticContract($"Property value target '{value.TargetProperty}' is duplicated.");
                }

                if (!targets.TryGetValue(value.TargetProperty, out var target))
                {
                    throw new InvalidSemanticContract($"Property value target '{value.TargetProperty}' is unresolved.");
                }

                ValidateValue(value.Value, target.Type, "property value");
            }

            if (requireExact && assigned.Count != targets.Count)
            {
                throw new InvalidSemanticContract("A specification property value shape must assign every target exactly once.");
            }
        }

        SemanticTypeReference? ResolveExpression(
            SemanticExpression expression,
            SemanticExpressionRootKind expectedRoot,
            Dictionary<SemanticId, SemanticProperty> properties)
        {
            RejectNull(expression, "expression");
            ValidateEnum(expression.Kind, SemanticExpressionKind.Unknown, "expression kind");
            switch (expression)
            {
                case SemanticValueExpression value when expression.Kind == SemanticExpressionKind.Value:
                    RejectNull(value.Value, "semantic value");
                    ValidateValueVariant(value.Value);
                    return TypeOf(value.Value);
                case SemanticEventContextExpression context when expression.Kind == SemanticExpressionKind.EventContext:
                    ValidateTypeReference(context.Type);
                    if (_semanticVersion == SemanticVersion.V1 ||
                        (expectedRoot != SemanticExpressionRootKind.Command && !_occurrenceRoots) ||
                        context.Value is not (SemanticEventContextValueKind.Occurred or SemanticEventContextValueKind.CausedBySubject or
                            SemanticEventContextValueKind.CausedByName or SemanticEventContextValueKind.CausedByUserName) ||
                        context.Type.IsCollection || context.Type.IsOptional)
                    {
                        throw new InvalidSemanticContract("Only scalar command occurrence fields are admitted in ESM v2 produces mappings.");
                    }

                    var primitive = context.Type.Kind == SemanticTypeReferenceKind.Concept
                        ? _concepts[context.Type.Target].Primitive : context.Type.Primitive;
                    if (context.Value == SemanticEventContextValueKind.Occurred
                        ? primitive != SemanticPrimitiveType.DateTime
                        : primitive is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid))
                    {
                        throw new InvalidSemanticContract("The occurrence context field and mapping target have incompatible types.");
                    }

                    return context.Type;
                case SemanticResolvedExpression resolved when expression.Kind == SemanticExpressionKind.Resolved:
                    ValidateEnum(resolved.Root, SemanticExpressionRootKind.Unknown, "expression root");
                    ValidateEnum(resolved.Source, SemanticExpressionSourceKind.Unknown, "expression source");
                    if (resolved.Root != expectedRoot || !resolved.Target.IsSet)
                    {
                        throw new InvalidSemanticContract("A resolved expression uses the wrong root or a default target identity.");
                    }

                    return resolved.Source switch
                    {
                        SemanticExpressionSourceKind.Property when properties.TryGetValue(resolved.Target, out var property) => property.Type,
                        _ => throw new InvalidSemanticContract($"Resolved expression target '{resolved.Target}' is unresolved in its root scope.")
                    };
                default:
                    throw new InvalidSemanticContract("A semantic expression variant is malformed or unknown.");
            }
        }

        void ValidateValue(SemanticValue value, SemanticTypeReference target, string description) =>
            _valueValidator.Validate(value, target, description);

        void ValidateValueVariant(SemanticValue value) => _valueValidator.ValidateVariant(value);

        SemanticTypeReference? TypeOf(SemanticValue value) => _valueValidator.TypeOf(value);

        void RequireCompatible(SemanticTypeReference? source, SemanticTypeReference target, string description)
        {
            if (source is null)
            {
                if (!target.IsOptional)
                {
                    throw new InvalidSemanticContract($"A null {description} source is incompatible with a required target.");
                }

                return;
            }

            if (!SameType(source, target) && !(SameValueType(source, target) && !source.IsOptional && target.IsOptional))
            {
                throw new InvalidSemanticContract($"A {description} source and target have incompatible types.");
            }
        }

        bool SameType(SemanticTypeReference left, SemanticTypeReference right) =>
            SameValueType(left, right) && left.IsCollection == right.IsCollection && left.IsOptional == right.IsOptional;

        bool SameValueType(SemanticTypeReference left, SemanticTypeReference right) =>
            left.Kind == right.Kind && left.Primitive == right.Primitive && left.Target == right.Target;

        SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type) => type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when _concepts.TryGetValue(type.Target, out var concept) => concept.Primitive,
            _ => SemanticPrimitiveType.Unknown
        };

        void RequireBoolean(SemanticTypeReference? type, string description)
        {
            if (type?.IsCollection is not false || type.IsOptional || UnderlyingPrimitive(type) != SemanticPrimitiveType.Boolean)
            {
                throw new InvalidSemanticContract($"A {description} must resolve to required scalar Boolean.");
            }
        }

        SemanticProperty IdentifierProperty(SemanticReadModel readModel)
        {
            var identifiers = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
            if (identifiers.Length != 1 || identifiers[0].Type.IsCollection || identifiers[0].Type.IsOptional)
            {
                throw new InvalidSemanticContract($"Read model '{readModel.Name}' must have exactly one required scalar identifier property.");
            }

            return identifiers[0];
        }

        Dictionary<SemanticId, SemanticProperty> Properties(ImmutableArray<SemanticProperty> properties) =>
            properties.ToDictionary(_ => _.Id);

        void Register(SemanticId id, string name, string description)
        {
            if (!id.IsSet)
            {
                throw new InvalidSemanticContract($"A {description} has a default semantic identity.");
            }

            RequireName(name, description);
            if (!_ids.Add(id))
            {
                throw new InvalidSemanticContract($"Semantic identity '{id}' is duplicated.");
            }
        }

        void RejectDuplicateReadModelStates(
            ImmutableArray<SemanticSpecificationReadModel> states,
            string description)
        {
            for (var first = 0; first < states.Length; first++)
            {
                for (var second = first + 1; second < states.Length; second++)
                {
                    if (states[first].ReadModel == states[second].ReadModel &&
                        SemanticValueRules.AreEqual(states[first].Key, states[second].Key))
                    {
                        throw new InvalidSemanticContract($"A specification contains a duplicated {description} state key.");
                    }
                }
            }
        }

        void RejectDuplicateAbsentReadModels(ImmutableArray<SemanticSpecificationAbsentReadModel> states)
        {
            for (var first = 0; first < states.Length; first++)
            {
                for (var second = first + 1; second < states.Length; second++)
                {
                    if (states[first].ReadModel == states[second].ReadModel &&
                        SemanticValueRules.AreEqual(states[first].Key, states[second].Key))
                    {
                        throw new InvalidSemanticContract("A specification contains a duplicated absent read model key.");
                    }
                }
            }
        }

        void RejectDuplicateQueryExpectations(ImmutableArray<SemanticSpecificationQueryResult> results)
        {
            for (var first = 0; first < results.Length; first++)
            {
                for (var second = first + 1; second < results.Length; second++)
                {
                    if (results[first].Query == results[second].Query &&
                        SemanticValueRules.AreEqual(results[first].Key, results[second].Key))
                    {
                        throw new InvalidSemanticContract("A specification contains a duplicated query expectation key.");
                    }
                }
            }
        }

        void RequireSpecificationArrays(SemanticSpecification specification)
        {
            RequireObjects(specification.GivenEvents, nameof(specification.GivenEvents), "specification event");
            RequireObjects(specification.GivenReadModels, nameof(specification.GivenReadModels), "specification read model");
            if (specification.When is not null)
            {
                RequireObjects(specification.When.Values, nameof(specification.When.Values), "property value");
            }
            if (specification.WhenAppended is not null) RequireObjects(specification.WhenAppended.Values, nameof(specification.WhenAppended.Values), "property value");
            RequireObjects(specification.ThenEvents, nameof(specification.ThenEvents), "specification event");
            RequireObjects(specification.ThenReadModels, nameof(specification.ThenReadModels), "specification read model");
            RequireObjects(specification.ThenAbsentReadModels, nameof(specification.ThenAbsentReadModels), "specification absent read model");
            RequireObjects(specification.ThenQueries, nameof(specification.ThenQueries), "specification query result");
            RequireObjects(specification.ThenErrors, nameof(specification.ThenErrors), "specification error");
        }

        IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
            application.Modules.SelectMany(_ => AllSlices(_.Features));

        IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
            features.SelectMany(_ => _.Slices.Concat(AllSlices(_.Features)));

        void ValidatePrimitive(SemanticPrimitiveType primitive) =>
            ValidateEnum(primitive, SemanticPrimitiveType.Unknown, "primitive type");

        void ValidateEnum<T>(T value, T unknown, string description)
            where T : struct, Enum
        {
            if (!Enum.IsDefined(value) || EqualityComparer<T>.Default.Equals(value, unknown))
            {
                throw new InvalidSemanticContract($"The {description} value '{Convert.ToInt32(value, CultureInfo.InvariantCulture)}' is unknown.");
            }
        }

        void RequireName(string name, string description)
        {
            if (string.IsNullOrEmpty(name) || !IsNfc(name))
            {
                throw new InvalidSemanticContract($"The {description} name must be non-empty, well-formed Unicode NFC text.");
            }
        }

        bool IsNfc(string value)
        {
            try
            {
                return value.IsNormalized(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        void RejectDuplicateNames(IEnumerable<string> names, string description)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in names)
            {
                RequireName(name, description);
                if (!seen.Add(name))
                {
                    throw new InvalidSemanticContract($"Duplicate {description} name '{name}' is ambiguous.");
                }
            }
        }

        void RequireArray<T>(ImmutableArray<T> values, string name)
        {
            if (values.IsDefault)
            {
                throw new InvalidSemanticContract($"The immutable array '{name}' cannot be default.");
            }
        }

        void RequireObjects<T>(ImmutableArray<T> values, string name, string description)
            where T : class
        {
            RequireArray(values, name);
            foreach (var value in values)
            {
                RejectNull(value, description);
            }
        }

        void RejectNull<T>(T? value, string description)
            where T : class
        {
            if (value is null)
            {
                throw new InvalidSemanticContract($"A {description} cannot be null.");
            }
        }
    }
}
