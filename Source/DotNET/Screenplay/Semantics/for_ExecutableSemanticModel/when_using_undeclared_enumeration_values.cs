// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_undeclared_enumeration_values : a_valid_semantic_model
{
    Exception _collectionSpecificationValue;
    Exception _invalidRepresentation;
    Exception _literalMapping;
    Exception _nestedSpecificationValue;
    Exception _specificationValue;
    Exception _validationOperand;

    void Because()
    {
        var stateChange = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
        var success = stateChange.Specifications.Single(_ => _.ThenEvents.Length > 0);
        var projectName = _application.Concepts.Single(_ => _.Id == _projectNameConceptId);
        var enumeration = projectName with { Values = ["Screenplay"] };
        var enumerationSlice = stateChange with { Specifications = [success] };

        _validationOperand = Validate(
            enumeration with
            {
                Validations = [.. enumeration.Validations, new(default, SemanticValidationRuleKind.Equal, SemanticValue.Text("Undeclared"), null)]
            },
            enumerationSlice);

        var command = enumerationSlice.Commands.Single();
        var produced = command.Produces.Single();
        var literal = new SemanticPropertyMapping(_eventNamePropertyId, SemanticExpression.FromValue(SemanticValue.Text("Undeclared")));
        var literalCommand = command with
        {
            Produces = [produced with
            {
                Mappings = [.. produced.Mappings.Select(_ => _.TargetProperty == _eventNamePropertyId ? literal : _)]
            }]
        };
        _literalMapping = Validate(enumeration, enumerationSlice with { Commands = [literalCommand] });

        var undeclaredWhen = success.When with
        {
            Values = [.. success.When.Values.Select(_ => _.TargetProperty == _commandNamePropertyId
                ? _ with { Value = SemanticValue.Text("Undeclared") }
                : _)]
        };
        _specificationValue = Validate(
            enumeration,
            enumerationSlice with { Specifications = [success with { When = undeclaredWhen }] });

        _collectionSpecificationValue = ValidateSpecificationValue(
            enumeration,
            SemanticTypeReference.ForConcept(enumeration.Id, isCollection: true),
            SemanticValue.Array([SemanticValue.Text("Screenplay"), SemanticValue.Text("Undeclared")]),
            []);
        var envelopeTypeId = Id(SemanticKind.CompositeType, "ProjectNameEnvelope");
        var envelopeNamePropertyId = Id(SemanticKind.Property, "ProjectNameEnvelope.Name");
        var envelopeType = new SemanticCompositeType(
            envelopeTypeId,
            "ProjectNameEnvelope",
            [new(envelopeNamePropertyId, "Name", SemanticTypeReference.ForConcept(enumeration.Id), false)]);
        _nestedSpecificationValue = ValidateSpecificationValue(
            enumeration,
            SemanticTypeReference.ForCompositeType(envelopeTypeId),
            SemanticValue.Composite([new(envelopeNamePropertyId, SemanticValue.Text("Undeclared"))]),
            [envelopeType]);

        var projectId = _application.Concepts.Single(_ => _.Id == _projectIdConceptId) with
        {
            Values = ["00000000-0000-0000-0000-000000000001"]
        };
        _invalidRepresentation = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            _application with
            {
                Concepts = [.. _application.Concepts.Select(_ => _.Id == projectId.Id ? projectId : _)]
            }));

        Exception Validate(SemanticConcept concept, SemanticSlice slice)
        {
            var application = ReplaceSlice(slice);
            application = application with
            {
                Concepts = [.. application.Concepts.Select(_ => _.Id == concept.Id ? concept : _)]
            };
            return Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application));
        }

        Exception ValidateSpecificationValue(
            SemanticConcept concept,
            SemanticTypeReference type,
            SemanticValue value,
            SemanticCompositeType[] additionalTypes)
        {
            var eventContract = enumerationSlice.Events.Single();
            var typedEvent = eventContract with
            {
                Properties = [.. eventContract.Properties.Select(_ => _.Id == _eventNamePropertyId ? _ with { Type = type } : _)]
            };
            var typedCommand = command with
            {
                Properties = [.. command.Properties.Select(_ => _.Id == _commandNamePropertyId ? _ with { Type = type } : _)]
            };
            var typedWhen = success.When with
            {
                Values = [.. success.When.Values.Select(_ => _.TargetProperty == _commandNamePropertyId ? _ with { Value = value } : _)]
            };
            var expectedEvent = success.ThenEvents.Single();
            var typedExpectedEvent = expectedEvent with
            {
                Values = [.. expectedEvent.Values.Select(_ => _.TargetProperty == _eventNamePropertyId ? _ with { Value = value } : _)]
            };
            var typedSpecification = success with
            {
                When = typedWhen,
                ThenEvents = [typedExpectedEvent],
                ThenReadModels = [],
                ThenQueries = []
            };
            var typedSlice = enumerationSlice with
            {
                Events = [typedEvent],
                Commands = [typedCommand],
                Specifications = [typedSpecification]
            };
            var module = _application.Modules.Single();
            var feature = module.Features.Single() with { Slices = [typedSlice] };
            var application = _application with
            {
                Concepts = [.. _application.Concepts.Select(_ => _.Id == concept.Id ? concept : _)],
                Types = [.. _application.Types, .. additionalTypes],
                Modules = [module with { Features = [feature] }]
            };
            return Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application));
        }
    }

    [Fact] void should_reject_an_enumeration_with_a_non_text_representation() => _invalidRepresentation.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_collection_element() => _collectionSpecificationValue.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_literal_mapping() => _literalMapping.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_nested_property() => _nestedSpecificationValue.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_specification_value() => _specificationValue.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_validation_operand() => _validationOperand.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
