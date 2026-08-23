// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
        if (languageVersion.Major == 0 || semanticVersion.Major == 0)
        {
            throw new InvalidSemanticContract("Language and semantic versions must have a positive major version.");
        }

        SemanticModelValidator.Validate(application);
        var withoutRevision = SemanticModelCanonicalJson.SerializeWithoutRevision(languageVersion, semanticVersion, application);
        var revision = SemanticRevision.Compute(withoutRevision);
        return new(languageVersion, semanticVersion, revision, application);
    }
}

static class SemanticModelValidator
{
    public static void Validate(SemanticApplication application)
    {
        if (application is null)
        {
            throw new InvalidSemanticContract("The semantic application cannot be null.");
        }

        var context = new ValidationContext();
        context.RegisterApplication(application);
        context.ValidateReferences(application);
    }

    sealed class ValidationContext
    {
        readonly HashSet<SemanticId> _ids = [];
        readonly Dictionary<SemanticId, SemanticConcept> _concepts = [];
        readonly Dictionary<SemanticId, SemanticCompositeType> _types = [];
        readonly Dictionary<EventContractId, SemanticEventContract> _events = [];
        readonly Dictionary<SemanticId, SemanticCommand> _commands = [];
        readonly Dictionary<SemanticId, SemanticReadModel> _readModels = [];
        readonly Dictionary<SemanticId, SemanticKeyedQuery> _queries = [];

        public void RegisterApplication(SemanticApplication application)
        {
            Register(application.Id, application.Name, "application");
            RequireArray(application.Concepts, nameof(application.Concepts));
            RequireArray(application.Types, nameof(application.Types));
            RequireArray(application.Modules, nameof(application.Modules));
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
                RequireArray(concept.Validations, nameof(concept.Validations));
                foreach (var validation in concept.Validations)
                {
                    ValidateValidation(validation, null);
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
        }

        void RegisterConcept(SemanticConcept concept)
        {
            RejectNull(concept, "concept");
            Register(concept.Id, concept.Name, "concept");
            RequireArray(concept.Values, nameof(concept.Values));
            RequireArray(concept.Validations, nameof(concept.Validations));
            RejectDuplicateNames(concept.Values, $"value on concept '{concept.Name}'");
            _concepts.Add(concept.Id, concept);
        }

        void RegisterType(SemanticCompositeType type)
        {
            RejectNull(type, "composite type");
            Register(type.Id, type.Name, "composite type");
            RegisterProperties(type.Properties, $"composite type '{type.Name}'");
            _types.Add(type.Id, type);
        }

        void RegisterModule(SemanticModule module)
        {
            RejectNull(module, "module");
            Register(module.Id, module.Name, "module");
            RequireArray(module.Features, nameof(module.Features));
            RejectDuplicateNames(module.Features.Select(_ => _.Name), $"feature in module '{module.Name}'");
            foreach (var feature in module.Features)
            {
                RegisterFeature(feature);
            }
        }

        void RegisterFeature(SemanticFeature feature)
        {
            RejectNull(feature, "feature");
            Register(feature.Id, feature.Name, "feature");
            RequireArray(feature.Features, nameof(feature.Features));
            RequireArray(feature.Slices, nameof(feature.Slices));
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
            RejectNull(slice, "slice");
            Register(slice.Id, slice.Name, "slice");
            RequireArray(slice.Events, nameof(slice.Events));
            RequireArray(slice.Commands, nameof(slice.Commands));
            RequireArray(slice.ReadModels, nameof(slice.ReadModels));
            RequireArray(slice.Projections, nameof(slice.Projections));
            RequireArray(slice.Queries, nameof(slice.Queries));
            RequireArray(slice.Specifications, nameof(slice.Specifications));
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
                RejectNull(projection, "projection");
                Register(projection.Id, projection.Name, "projection");
                RequireArray(projection.Transitions, nameof(projection.Transitions));
            }

            foreach (var query in slice.Queries)
            {
                RejectNull(query, "query");
                Register(query.Id, query.Name, "query");
                _queries.Add(query.Id, query);
            }

            foreach (var specification in slice.Specifications)
            {
                RejectNull(specification, "specification");
                Register(specification.Id, specification.Name, "specification");
                RequireSpecificationArrays(specification);
            }
        }

        void RegisterEvent(SemanticEventContract eventContract)
        {
            RejectNull(eventContract, "event contract");
            Register(eventContract.Id, eventContract.Name, "event contract");
            if (!eventContract.ContractId.IsSet || eventContract.Revision != EventContractRevision.Initial)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Name}' must use the initial ESM v1 contract revision.");
            }

            if (!_events.TryAdd(eventContract.ContractId, eventContract))
            {
                throw new InvalidSemanticContract($"Event contract identity '{eventContract.ContractId}' is duplicated.");
            }

            RegisterProperties(eventContract.Properties, $"event contract '{eventContract.Name}'");
        }

        void RegisterCommand(SemanticCommand command)
        {
            RejectNull(command, "command");
            Register(command.Id, command.Name, "command");
            RegisterProperties(command.Properties, $"command '{command.Name}'");
            RequireArray(command.Validations, nameof(command.Validations));
            RequireArray(command.Produces, nameof(command.Produces));
            _commands.Add(command.Id, command);
        }

        void RegisterReadModel(SemanticReadModel readModel)
        {
            RejectNull(readModel, "read model");
            Register(readModel.Id, readModel.Name, "read model");
            RegisterProperties(readModel.Properties, $"read model '{readModel.Name}'");
            _readModels.Add(readModel.Id, readModel);
        }

        void RegisterProperties(ImmutableArray<SemanticProperty> properties, string owner)
        {
            RequireArray(properties, nameof(properties));
            RejectDuplicateNames(properties.Select(_ => _.Name), $"property on {owner}");
            foreach (var property in properties)
            {
                RejectNull(property, "property");
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
                ValidateProperties(command.Properties);
                HashSet<SemanticId> commandProperties = [.. command.Properties.Select(_ => _.Id)];
                foreach (var validation in command.Validations)
                {
                    ValidateValidation(validation, commandProperties);
                }

                foreach (var produced in command.Produces)
                {
                    ValidateProducedEvent(produced);
                }
            }

            foreach (var readModel in slice.ReadModels)
            {
                ValidateProperties(readModel.Properties);
            }

            foreach (var projection in slice.Projections)
            {
                ValidateProjection(projection);
            }

            foreach (var query in slice.Queries)
            {
                ValidateQuery(query);
            }

            foreach (var specification in slice.Specifications)
            {
                ValidateSpecification(specification);
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

        void ValidateValidation(SemanticValidationRule validation, HashSet<SemanticId>? allowedProperties)
        {
            RejectNull(validation, "validation rule");
            ValidateEnum(validation.Kind, SemanticValidationRuleKind.Unknown, "validation rule kind");
            if (allowedProperties is null)
            {
                if (validation.Property.IsSet)
                {
                    throw new InvalidSemanticContract("A concept validation must use the implicit concept value.");
                }
            }
            else if (!allowedProperties.Contains(validation.Property))
            {
                throw new InvalidSemanticContract($"Validation property '{validation.Property}' is unresolved.");
            }

            if (validation.Operand is not null)
            {
                ValidateExpression(validation.Operand);
            }
        }

        void ValidateProducedEvent(SemanticProducedEvent produced)
        {
            RejectNull(produced, "produced event");
            if (!_events.TryGetValue(produced.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Produced event contract '{produced.EventContract}' is unresolved.");
            }

            ValidateOptionalExpression(produced.Condition);
            ValidateOptionalExpression(produced.Destination);
            HashSet<SemanticId> properties = [.. eventContract.Properties.Select(_ => _.Id)];
            ValidateMappings(produced.Mappings, properties);
        }

        void ValidateProjection(SemanticProjection projection)
        {
            if (!_readModels.TryGetValue(projection.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Projection read model '{projection.ReadModel}' is unresolved.");
            }

            HashSet<SemanticId> properties = [.. readModel.Properties.Select(_ => _.Id)];
            foreach (var transition in projection.Transitions)
            {
                RejectNull(transition, "projection transition");
                if (!_events.ContainsKey(transition.EventContract))
                {
                    throw new InvalidSemanticContract($"Projection event contract '{transition.EventContract}' is unresolved.");
                }

                RejectNull(transition.AffectedInstance, "affected instance");
                ValidateEnum(transition.AffectedInstance.Cardinality, AffectedInstanceCardinality.Unknown, "affected instance cardinality");
                ValidateExpression(transition.AffectedInstance.Key);
                ValidateMappings(transition.Mappings, properties);
            }
        }

        void ValidateQuery(SemanticKeyedQuery query)
        {
            if (!_readModels.TryGetValue(query.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Query read model '{query.ReadModel}' is unresolved.");
            }

            RejectNull(query.Argument, "query argument");
            RequireName(query.Argument.Name, "query argument");
            ValidateTypeReference(query.Argument.Type);
            if (!readModel.Properties.Any(_ => _.Id == query.KeyProperty))
            {
                throw new InvalidSemanticContract($"Query key property '{query.KeyProperty}' is unresolved.");
            }

            ValidateEnum(query.Cardinality, SemanticQueryCardinality.Unknown, "query cardinality");
            ValidateEnum(query.Delivery, SemanticQueryDelivery.Unknown, "query delivery");
        }

        void ValidateSpecification(SemanticSpecification specification)
        {
            if (!_commands.TryGetValue(specification.When.Command, out var command))
            {
                throw new InvalidSemanticContract($"Specification command '{specification.When.Command}' is unresolved.");
            }

            HashSet<SemanticId> commandProperties = [.. command.Properties.Select(_ => _.Id)];
            ValidateMappings(specification.When.Values, commandProperties);
            foreach (var value in specification.GivenEvents.Concat(specification.ThenEvents))
            {
                ValidateSpecificationEvent(value);
            }

            foreach (var state in specification.GivenReadModels.Concat(specification.ThenReadModels))
            {
                ValidateSpecificationReadModel(state);
            }

            foreach (var result in specification.ThenQueries)
            {
                RejectNull(result, "specification query result");
                if (!_queries.TryGetValue(result.Query, out var query))
                {
                    throw new InvalidSemanticContract($"Specification query '{result.Query}' is unresolved.");
                }

                ValidateExpression(result.Key);
                RequireArray(result.Results, nameof(result.Results));
                foreach (var state in result.Results)
                {
                    if (state.ReadModel != query.ReadModel)
                    {
                        throw new InvalidSemanticContract("A specification query result uses the wrong read model.");
                    }

                    ValidateSpecificationReadModel(state);
                }
            }

            foreach (var error in specification.ThenErrors)
            {
                RejectNull(error, "specification error");
                if (error.Code is null && error.Message is null)
                {
                    continue;
                }

                if (error.Code?.Length == 0 || error.Message?.Length == 0)
                {
                    throw new InvalidSemanticContract("Specification error code and message cannot be empty strings.");
                }
            }
        }

        void ValidateSpecificationEvent(SemanticSpecificationEvent value)
        {
            RejectNull(value, "specification event");
            if (!_events.TryGetValue(value.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Specification event '{value.EventContract}' is unresolved.");
            }

            HashSet<SemanticId> eventProperties = [.. eventContract.Properties.Select(_ => _.Id)];
            ValidateMappings(value.Values, eventProperties);
        }

        void ValidateSpecificationReadModel(SemanticSpecificationReadModel state)
        {
            RejectNull(state, "specification read model");
            if (!_readModels.TryGetValue(state.ReadModel, out var readModel))
            {
                throw new InvalidSemanticContract($"Specification read model '{state.ReadModel}' is unresolved.");
            }

            ValidateExpression(state.Key);
            HashSet<SemanticId> readModelProperties = [.. readModel.Properties.Select(_ => _.Id)];
            ValidateMappings(state.Values, readModelProperties);
        }

        void ValidateMappings(ImmutableArray<SemanticPropertyMapping> mappings, HashSet<SemanticId> allowedProperties)
        {
            RequireArray(mappings, nameof(mappings));
            foreach (var mapping in mappings)
            {
                RejectNull(mapping, "property mapping");
                if (!allowedProperties.Contains(mapping.TargetProperty))
                {
                    throw new InvalidSemanticContract($"Mapping target property '{mapping.TargetProperty}' is unresolved.");
                }

                ValidateExpression(mapping.Source);
            }
        }

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

        void RequireSpecificationArrays(SemanticSpecification specification)
        {
            RequireArray(specification.GivenEvents, nameof(specification.GivenEvents));
            RequireArray(specification.GivenReadModels, nameof(specification.GivenReadModels));
            RejectNull(specification.When, "specification command");
            RequireArray(specification.When.Values, nameof(specification.When.Values));
            RequireArray(specification.ThenEvents, nameof(specification.ThenEvents));
            RequireArray(specification.ThenReadModels, nameof(specification.ThenReadModels));
            RequireArray(specification.ThenQueries, nameof(specification.ThenQueries));
            RequireArray(specification.ThenErrors, nameof(specification.ThenErrors));
        }

        IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
            application.Modules.SelectMany(_ => AllSlices(_.Features));

        IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
            features.SelectMany(_ => _.Slices.Concat(AllSlices(_.Features)));

        void ValidateExpression(SemanticExpression expression)
        {
            RejectNull(expression, "expression");
            var valid = expression.Kind switch
            {
                SemanticExpressionKind.Null => expression.Text is null && expression.Number is null && expression.Boolean is null,
                SemanticExpressionKind.Text => expression.Text is not null && expression.Number is null && expression.Boolean is null,
                SemanticExpressionKind.Number => expression.Text is null && expression.Number is not null && expression.Boolean is null,
                SemanticExpressionKind.Boolean => expression.Text is null && expression.Number is null && expression.Boolean is not null,
                SemanticExpressionKind.Path => !string.IsNullOrEmpty(expression.Text) && expression.Number is null && expression.Boolean is null,
                _ => false
            };
            if (!valid)
            {
                throw new InvalidSemanticContract("A semantic expression is malformed or unknown.");
            }
        }

        void ValidateOptionalExpression(SemanticExpression? expression)
        {
            if (expression is not null)
            {
                ValidateExpression(expression);
            }
        }

        void ValidatePrimitive(SemanticPrimitiveType primitive) =>
            ValidateEnum(primitive, SemanticPrimitiveType.Unknown, "primitive type");

        void ValidateEnum<T>(T value, T unknown, string description)
            where T : struct, Enum
        {
            if (!Enum.IsDefined(value) || EqualityComparer<T>.Default.Equals(value, unknown))
            {
                throw new InvalidSemanticContract($"The {description} value '{Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}' is unknown.");
            }
        }

        void RequireName(string name, string description)
        {
            if (string.IsNullOrEmpty(name) || !name.IsNormalized(NormalizationForm.FormC))
            {
                throw new InvalidSemanticContract($"The {description} name must be non-empty Unicode NFC text.");
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
