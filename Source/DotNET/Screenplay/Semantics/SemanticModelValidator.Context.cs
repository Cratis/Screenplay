// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
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
        readonly List<SemanticId> _subjects = [];
        readonly List<SemanticValidationFailure>? _failures;

        readonly SemanticVersion _semanticVersion;

        readonly int _utcAssessmentYear;

        public ValidationContext(SemanticVersion semanticVersion, SemanticApplication application, int? utcAssessmentYear = null, List<SemanticValidationFailure>? failures = null)
        {
            _failures = failures;
            _eventRoutesApplication = application;
            _semanticVersion = semanticVersion;
            _utcAssessmentYear = utcAssessmentYear ?? DateTimeOffset.UtcNow.Year;
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
                ValidateOwner(concept.Id, () => ValidateConceptReferences(concept));
            }

            foreach (var type in application.Types)
            {
                ValidateOwner(type.Id, () => ValidateProperties(type.Properties));
            }

            ValidateBoundary(ValidateEventSources);
            ValidateBoundary(ValidateTriggers);
            foreach (var slice in AllSlices(application))
            {
                ValidateOwner(slice.Id, () => ValidateSlice(slice));
            }

            ValidateBoundary(() => ValidatePolicies(application));
        }

        void ValidateOwner(SemanticId subject, Action validate) => ValidateBoundary(() =>
        {
            _subjects.Add(subject);
            validate();
            _subjects.RemoveAt(_subjects.Count - 1);
        });

        void ValidateBoundary(Action validate)
        {
            var depth = _subjects.Count;
            try
            {
                validate();
            }
            catch (InvalidSemanticContract exception) when (_failures is not null)
            {
                _failures.Add(new([.. _subjects], exception.Message));
                _subjects.RemoveRange(depth, _subjects.Count - depth);
            }
        }

        void ValidateConceptReferences(SemanticConcept concept)
        {
            ValidatePrimitive(concept.Primitive);
            if (!concept.Values.IsEmpty && concept.Primitive != SemanticPrimitiveType.Text)
            {
                throw new InvalidSemanticContract($"Enumeration concept '{concept.Name}' must use the text primitive representation.");
            }

            ValidateCompliance(concept);
            ValidateEnumMemberNumbers(concept);
            var conceptType = SemanticTypeReference.ForConcept(concept.Id);
            foreach (var validation in concept.Validations)
            {
                ValidateValidation(validation, true, conceptType);
            }

            RejectDuplicateRequirementIds(concept.Validations.Select(validation => validation.RequirementId), $"Concept '{concept.Name}'");
        }

        void ValidateSlice(SemanticSlice slice)
        {
            ValidateEnum(slice.Kind, SemanticSliceKind.Unknown, "slice kind");
            ValidatePublicEventSlice(slice);
            ValidateSliceContracts(slice);
            ValidateSliceBehaviors(slice);
            ValidateConstraints(slice);
            ValidateAutomation(slice);
        }

        void ValidateSliceContracts(SemanticSlice slice)
        {
            foreach (var eventContract in slice.Events)
            {
                ValidateOwner(eventContract.Id, () =>
                {
                    ValidateProperties(eventContract.Properties);
                    foreach (var prior in eventContract.PriorRevisions)
                    {
                        ValidateProperties(prior.Properties);
                    }
                });
            }

            foreach (var command in slice.Commands)
            {
                ValidateOwner(command.Id, () => ValidateCommand(command));
            }

            foreach (var readModel in slice.ReadModels)
            {
                ValidateOwner(readModel.Id, () =>
                {
                    ValidateProperties(readModel.Properties);
                    ReadModelKeyProperties(readModel);
                });
            }
        }

        void ValidateSliceBehaviors(SemanticSlice slice)
        {
            foreach (var projection in slice.Projections)
            {
                ValidateOwner(projection.Id, () => ValidateProjection(projection));
            }

            foreach (var reducer in slice.Reducers)
            {
                // Reducers carry no semantic identity; their enclosing slice remains the located subject.
                ValidateBoundary(() => ValidateReducer(reducer));
            }

            foreach (var query in slice.Queries)
            {
                ValidateOwner(query.Id, () => ValidateQuery(query));
            }

            foreach (var specification in slice.Specifications)
            {
                ValidateOwner(specification.Id, () => ValidateSpecification(specification));
            }
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

        IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
            application.Modules.SelectMany(_ => AllSlices(_.Features));

        IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
            features.SelectMany(_ => _.Slices.Concat(AllSlices(_.Features)));

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
