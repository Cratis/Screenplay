// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the immutable, versioned executable semantic model.
/// </summary>
public sealed record ExecutableSemanticModel
{
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
        EsmSchemaV3Support.EnsureSupported(languageVersion, semanticVersion);
        SemanticModelValidator.Validate(application, semanticVersion);
        var withoutRevision = SemanticModelCanonicalJson.SerializeWithoutRevision(languageVersion, semanticVersion, application);
        var revision = SemanticRevision.Compute(withoutRevision);
        return new(languageVersion, semanticVersion, revision, application);
    }
}

internal static partial class SemanticModelValidator
{
    public static void Validate(SemanticApplication application, SemanticVersion semanticVersion = default)
    {
        if (application is null)
        {
            throw new InvalidSemanticContract("The semantic application cannot be null.");
        }

        var context = new ValidationContext(semanticVersion == default ? SemanticVersion.V1 : semanticVersion);
        context.RegisterApplication(application);
        context.ValidateReferences(application);
        if (semanticVersion == SemanticVersion.V2 && !application.Modules.SelectMany(module => module.Features)
            .SelectMany(AllSlices).Any(slice => slice.Commands.Any(command => command.Destination is not null ||
                command.Produces.Any(produced => produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))) ||
                slice.Specifications.Any(specification => specification.When?.EventSource is not null || specification.WhenAppended?.EventSource is not null ||
                    specification.GivenEvents.Any(value => value.EventSource is not null) ||
                    specification.ThenEvents.Any(value => value.EventSource is not null))))
        {
            throw new InvalidSemanticContract("An ESM v2 model must contain a typed destination, a specification event source, or an occurrence context mapping.");
        }

        if (semanticVersion == SemanticVersion.V3 && application.Modules.SelectMany(module => module.Features)
            .SelectMany(AllSlices).All(slice => slice.Reducers.IsEmpty))
        {
            throw new InvalidSemanticContract("An ESM v3 model must contain a reducer with transition bodies.");
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

        public ValidationContext(SemanticVersion semanticVersion)
        {
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
            }

            foreach (var type in application.Types)
            {
                ValidateProperties(type.Properties);
            }

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
                RejectNull(query.Argument, "query argument");
                Register(query.Argument.Id, query.Argument.Name, "query argument");
                _queries.Add(query.Id, query);
            }

            foreach (var specification in slice.Specifications)
            {
                Register(specification.Id, specification.Name, "specification");
                RequireSpecificationArrays(specification);
            }

            RegisterConstraints(slice);
        }

        void RegisterEvent(SemanticEventContract eventContract)
        {
            Register(eventContract.Id, eventContract.Name, "event contract");
            if (!eventContract.ContractId.IsSet || eventContract.Revision != EventContractRevision.Initial)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' must use the initial ESM v1 contract revision.");
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
            foreach (var eventContract in slice.Events)
            {
                ValidateProperties(eventContract.Properties);
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
        }

        void ValidateCommand(SemanticCommand command)
        {
            if (command.Destination is not null)
            {
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

            ValidateProperties(command.Properties);
            var properties = Properties(command.Properties);
            if (command.Requirements.IsDefault) throw new InvalidSemanticContract("Command requirements cannot be default.");
            foreach (var requirement in command.Requirements)
            {
                if (requirement is null || requirement.Message?.Length == 0) throw new InvalidSemanticContract("A requirement or its message is invalid.");
                ValidateEnum(requirement.Severity, (SemanticValidationSeverity)(-1), "requirement severity");
                ValidateCondition(requirement.Condition, properties);
            }

            foreach (var validation in command.Validations)
            {
                if (!properties.TryGetValue(validation.Property, out var property))
                {
                    throw new InvalidSemanticContract($"Validation property '{validation.Property}' is unresolved.");
                }

                ValidateValidation(validation, false, property.Type);
            }

            foreach (var produced in command.Produces)
            {
                ValidateProducedEvent(produced, command);
            }
        }

        void ValidateProperties(ImmutableArray<SemanticProperty> properties)
        {
            foreach (var property in properties)
            {
                ValidateTypeReference(property.Type);
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

        void ValidateValidation(
            SemanticValidationRule validation,
            bool isConcept,
            SemanticTypeReference propertyType)
        {
            RejectNull(validation, "validation rule");
            ValidateEnum(validation.Kind, SemanticValidationRuleKind.Unknown, "validation rule kind");
            ValidateEnum(validation.Severity, (SemanticValidationSeverity)(-1), "validation severity");
            if (isConcept && validation.Property.IsSet)
            {
                throw new InvalidSemanticContract("A concept validation must use the implicit concept value.");
            }

            if (validation.Message?.Length == 0)
            {
                throw new InvalidSemanticContract("A validation message cannot be empty.");
            }

            var requiresOperand = validation.Kind != SemanticValidationRuleKind.NotEmpty;
            if (requiresOperand != (validation.Operand is not null))
            {
                throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' has an invalid operand shape.");
            }

            var primitive = UnderlyingPrimitive(propertyType);
            var scalar = !propertyType.IsCollection;
            var number = primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;
            var text = primitive == SemanticPrimitiveType.Text && !IsEnumeration(propertyType);
            switch (validation.Kind)
            {
                case SemanticValidationRuleKind.NotEmpty when scalar && primitive != SemanticPrimitiveType.Text:
                    throw new InvalidSemanticContract("Not-empty validation requires text or a collection.");
                case SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum when !scalar || !(number || text):
                    throw new InvalidSemanticContract("Minimum and maximum validation require a scalar number or a scalar non-enumerated text value.");
                case SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual
                    when !scalar || !(number || primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean):
                    throw new InvalidSemanticContract("Equality validation requires a scalar text, enumeration, number or boolean value.");
                case SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
                    SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual when !scalar || !number:
                    throw new InvalidSemanticContract("Ordering validation requires a scalar number.");
                case SemanticValidationRuleKind.Length when !scalar || !text:
                    throw new InvalidSemanticContract("Length validation requires a scalar non-enumerated text value.");
                case SemanticValidationRuleKind.Matches when !scalar || !text:
                    throw new InvalidSemanticContract("Matches validation requires a scalar non-enumerated text value.");
                case SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual when scalar || !number:
                    throw new InvalidSemanticContract("Quantified validation requires a collection of numbers.");
            }

            if (validation.Operand is not null)
            {
                if (validation.Operand is SemanticNullValue && validation.Kind is not (SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual))
                {
                    throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' cannot use a null operand.");
                }

                // A length rule and a bound on text constrain its length, so the operand is a length, not text.
                var bindsLength = validation.Kind == SemanticValidationRuleKind.Length ||
                    (text && validation.Kind is SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum);
                var operandType = validation.Kind switch
                {
                    _ when bindsLength => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber),
                    SemanticValidationRuleKind.Matches => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text),
                    SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual =>
                        propertyType with { IsCollection = false, IsOptional = false },
                    _ => propertyType
                };
                ValidateValue(validation.Operand, operandType, "validation operand");
                if (validation.Kind == SemanticValidationRuleKind.Matches)
                {
                    if (validation.Operand is not SemanticTextValue pattern)
                    {
                        throw new InvalidSemanticContract("Matches validation requires a text pattern.");
                    }

                    try
                    {
                        _ = SemanticMatchPattern.Create(pattern.Value);
                    }
                    catch (ArgumentException exception)
                    {
                        throw new InvalidSemanticContract($"Matches validation has an invalid ECMAScript pattern: {exception.Message}");
                    }
                }

                if (bindsLength && validation.Operand is SemanticNumberValue { Value: < 0 })
                {
                    throw new InvalidSemanticContract($"Validation rule '{validation.Kind}' bounds a length and cannot use a negative operand.");
                }
            }
        }

        bool IsEnumeration(SemanticTypeReference type) =>
            type.Kind == SemanticTypeReferenceKind.Concept &&
            _concepts.TryGetValue(type.Target, out var concept) &&
            !concept.Values.IsEmpty;

        void ValidateProducedEvent(SemanticProducedEvent produced, SemanticCommand command)
        {
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
                var destinationType = ResolveExpression(produced.Destination, SemanticExpressionRootKind.Command, sources);
                var destinationProperty = produced.Destination is SemanticResolvedExpression
                {
                    Source: SemanticExpressionSourceKind.Property
                } resolved && sources.TryGetValue(resolved.Target, out var property)
                    ? property
                    : null;
                if (destinationType?.IsCollection is not false || destinationType.IsOptional || destinationProperty?.IsIdentifier is not true)
                {
                    throw new InvalidSemanticContract("A produced event destination must resolve to one required scalar command identity.");
                }
            }

            ValidateMappings(produced.Mappings, Properties(eventContract.Properties), SemanticExpressionRootKind.Command, sources);
            var mapped = produced.Mappings.Select(_ => _.TargetProperty).ToHashSet();
            if (eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateProjection(SemanticProjection projection)
        {
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
                if (!_events.TryGetValue(transition.EventContract, out var eventContract))
                {
                    throw new InvalidSemanticContract($"Projection event contract '{transition.EventContract}' is unresolved.");
                }

                RejectNull(transition.AffectedInstance, "affected instance");
                ValidateEnum(transition.AffectedInstance.Cardinality, AffectedInstanceCardinality.Unknown, "affected instance cardinality");
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
        }

        void ValidateReducer(SemanticReducer reducer)
        {
            if (_semanticVersion != SemanticVersion.V3 || string.IsNullOrWhiteSpace(reducer.Name) ||
                !_readModels.ContainsKey(reducer.ReadModel) || reducer.Transitions.IsDefaultOrEmpty)
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

            ValidateTypeReference(query.Argument.Type);
            if (!Properties(readModel.Properties).TryGetValue(query.KeyProperty, out var keyProperty))
            {
                throw new InvalidSemanticContract($"Query key property '{query.KeyProperty}' is unresolved.");
            }

            ValidateEnum(query.Cardinality, SemanticQueryCardinality.Unknown, "query cardinality");
            ValidateEnum(query.Delivery, SemanticQueryDelivery.Unknown, "query delivery");
            if (query.Argument.Type.IsCollection || keyProperty.Type.IsCollection || !SameType(query.Argument.Type, keyProperty.Type))
            {
                throw new InvalidSemanticContract("A query argument and key property must have the same scalar type and optionality.");
            }

            if (keyProperty.IsIdentifier == (query.Cardinality == SemanticQueryCardinality.Many))
            {
                throw new InvalidSemanticContract("A query cardinality is incompatible with whether its key property is an identifier.");
            }
        }

        void ValidateSpecification(SemanticSpecification specification)
        {
            SemanticCommand? command = null;
            if (specification.When is not null)
            {
                if (!_commands.TryGetValue(specification.When.Command, out command))
                {
                    throw new InvalidSemanticContract($"Specification command '{specification.When.Command}' is unresolved.");
                }

                if (specification.When.EventSource is not null)
                {
                    if (_semanticVersion == SemanticVersion.V1) throw new InvalidSemanticContract("A specification command event source requires ESM v2.");
                    ValidateEventSource(
                        specification.When.EventSource,
                        command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type);
                }

                ValidatePropertyValues(specification.When.Values, command.Properties, true);
            }

            if (specification.WhenAppended is { } appended)
            {
                if (specification.When is not null) throw new InvalidSemanticContract("A specification cannot state both command and append actions.");
                if (!_events.TryGetValue(appended.EventContract, out var eventContract)) throw new InvalidSemanticContract("An appended specification event is unresolved.");
                ValidatePropertyValues(appended.Values, eventContract.Properties, true);
                if (appended.EventSource is not null)
                {
                    if (_semanticVersion == SemanticVersion.V1) throw new InvalidSemanticContract("An appended event source requires ESM v2.");
                    var destinationTypes = _commands.Values.SelectMany(value => value.Produces
                        .Where(produced => produced.EventContract == appended.EventContract)
                        .Select(_ => value.Destination?.Type ?? value.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type))
                        .OfType<SemanticTypeReference>().Distinct().ToArray();
                    if (destinationTypes.Length != 1) throw new InvalidSemanticContract("An appended event source requires one unambiguous scalar destination type.");
                    ValidateEventSource(appended.EventSource, destinationTypes[0]);
                }
            }

            var producedEvents = command?.Produces.Select(_ => _.EventContract).ToHashSet();
            foreach (var value in specification.GivenEvents)
            {
                ValidateSpecificationEvent(value);
            }

            foreach (var value in specification.ThenEvents)
            {
                if (producedEvents?.Contains(value.EventContract) != true && specification.WhenAppended?.EventContract != value.EventContract)
                {
                    throw new InvalidSemanticContract("A specification expects an event the command does not produce.");
                }

                ValidateSpecificationEvent(value);
            }

            foreach (var state in specification.GivenReadModels)
            {
                ValidateSpecificationReadModel(state, true, true);
            }

            RejectDuplicateReadModelStates(specification.GivenReadModels, "given read model");
            foreach (var state in specification.ThenReadModels)
            {
                ValidateSpecificationReadModel(state, false, true);
            }

            RejectDuplicateReadModelStates(specification.ThenReadModels, "expected read model");
            foreach (var result in specification.ThenQueries)
            {
                ValidateSpecificationQuery(result);
            }

            RejectDuplicateQueryExpectations(specification.ThenQueries);

            foreach (var error in specification.ThenErrors)
            {
                ValidateSpecificationError(error);
            }

            if (specification.GivenCaller is { } caller)
            {
                RequireObjects(caller.Roles, nameof(caller.Roles), "caller role");
                RequireObjects(caller.Claims, nameof(caller.Claims), "caller claim");
            }

            var deniedQuery = specification.ThenDenied && specification.When is null && specification.WhenAppended is null &&
                specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty;
            var hasRejection = specification.ThenErrors.Length > 0 || specification.ThenDenied;
            var hasSuccessOutcome = specification.ThenEvents.Length > 0 || specification.ThenReadModels.Length > 0 ||
                (specification.ThenQueries.Length > 0 && !deniedQuery);
            if (hasRejection && (specification.ThenErrors.Length + (specification.ThenDenied ? 1 : 0) != 1 || hasSuccessOutcome))
            {
                throw new InvalidSemanticContract("A rejection specification must contain exactly one rejection and no success outcomes.");
            }

            if (specification.When is null && specification.WhenAppended is null && (specification.ThenErrors.Length > 0 || specification.ThenEvents.Length > 0 ||
                (specification.ThenDenied && !deniedQuery) ||
                (specification.ThenReadModels.Length == 0 && specification.ThenQueries.Length == 0)))
            {
                throw new InvalidSemanticContract("A specification without a command requires a read model or query outcome and cannot assert events or errors.");
            }

            if (!hasRejection && !hasSuccessOutcome)
            {
                throw new InvalidSemanticContract("A success specification must contain at least one success outcome.");
            }
        }

        void ValidateSpecificationEvent(SemanticSpecificationEvent value)
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
            if (value.EventSource is not null)
            {
                var producerTypes = _commands.Values.SelectMany(command => command.Produces
                    .Where(produced => produced.EventContract == value.EventContract)
                    .Select(_ => command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type))
                    .OfType<SemanticTypeReference>().Distinct().ToArray();
                if (producerTypes.Length != 1)
                {
                    throw new InvalidSemanticContract("A specification event source needs one unambiguous declared producer destination type.");
                }

                ValidateEventSource(value.EventSource, producerTypes[0]);
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

            ValidateValue(result.Key, query.Argument.Type, "specification query key");
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

            var keyProperty = _readModels[query.ReadModel].Properties.Single(_ => _.Id == query.KeyProperty);
            foreach (var state in result.Results)
            {
                if (state.ReadModel != query.ReadModel)
                {
                    throw new InvalidSemanticContract("A specification query result uses the wrong read model or key.");
                }

                ValidateSpecificationReadModel(state, false, false);
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

                if (mapping.Source is SemanticValueExpression value)
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
                    if (_semanticVersion == SemanticVersion.V1 || expectedRoot != SemanticExpressionRootKind.Command ||
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
