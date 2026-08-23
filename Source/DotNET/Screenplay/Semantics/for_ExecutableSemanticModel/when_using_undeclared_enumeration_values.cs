// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_undeclared_enumeration_values : a_valid_semantic_model
{
    Exception _invalidRepresentation;
    Exception _literalMapping;
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
    }

    [Fact] void should_reject_an_enumeration_with_a_non_text_representation() => _invalidRepresentation.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_literal_mapping() => _literalMapping.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_specification_value() => _specificationValue.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_undeclared_validation_operand() => _validationOperand.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
