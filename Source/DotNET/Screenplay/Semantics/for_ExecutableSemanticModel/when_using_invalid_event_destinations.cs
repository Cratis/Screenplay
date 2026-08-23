// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_event_destinations : a_valid_semantic_model
{
    Exception _literalDestination;
    Exception _nonIdentifierDestination;
    Exception _optionalDestination;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single();
        var produced = command.Produces.Single();

        _literalDestination = Validate(command with
        {
            Produces = [produced with
            {
                Destination = SemanticExpression.FromValue(SemanticValue.Text("00000000-0000-0000-0000-000000000001"))
            }]
        });
        _nonIdentifierDestination = Validate(command with
        {
            Produces = [produced with
            {
                Destination = SemanticExpression.Property(SemanticExpressionRootKind.Command, _commandNamePropertyId)
            }]
        });

        var optionalDestinationId = Id(SemanticKind.Property, "RegisterProject.OptionalDestination");
        var optionalDestination = new SemanticProperty(
            optionalDestinationId,
            "OptionalDestination",
            SemanticTypeReference.ForConcept(_projectIdConceptId, isOptional: true),
            true);
        _optionalDestination = Validate(command with
        {
            Properties = [.. command.Properties, optionalDestination],
            Produces = [produced with
            {
                Destination = SemanticExpression.Property(SemanticExpressionRootKind.Command, optionalDestinationId)
            }]
        });

        Exception Validate(SemanticCommand replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [replacement] })));
    }

    [Fact] void should_reject_a_literal_destination() => _literalDestination.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_non_identifier_destination() => _nonIdentifierDestination.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_optional_destination() => _optionalDestination.ShouldBeOfExactType<InvalidSemanticContract>();
}
