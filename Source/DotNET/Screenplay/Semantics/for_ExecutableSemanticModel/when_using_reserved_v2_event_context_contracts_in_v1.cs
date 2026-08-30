// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_reserved_v2_event_context_contracts_in_v1 : a_valid_semantic_model
{
    Exception _commandDestinationException;
    Exception _specificationCommandSourceException;
    Exception _specificationEventSourceException;

    void Because()
    {
        var stateChange = StateChange;
        var command = stateChange.Commands.Single() with
        {
            Destination = new(SemanticTypeReference.ForConcept(_projectIdConceptId), null)
        };
        _commandDestinationException = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(stateChange with { Commands = [command] })));

        var specification = stateChange.Specifications.Single(value => value.ThenEvents.Length > 0);
        var identity = new SemanticEventSourceIdentity(
            SemanticTypeReference.ForConcept(_projectIdConceptId),
            SemanticValue.Text("00000000-0000-0000-0000-000000000001"));
        var commandSourceSpecification = specification with
        {
            When = specification.When with { EventSource = identity }
        };
        _specificationCommandSourceException = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(ReplaceSpecification(stateChange, commandSourceSpecification))));

        var eventSourceSpecification = specification with
        {
            ThenEvents = [specification.ThenEvents.Single() with { EventSource = identity }]
        };
        _specificationEventSourceException = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(ReplaceSpecification(stateChange, eventSourceSpecification))));
    }

    [Fact] void should_reject_a_command_destination() => _commandDestinationException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_specification_command_source() => _specificationCommandSourceException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_specification_event_source() => _specificationEventSourceException.ShouldBeOfExactType<InvalidSemanticContract>();

    SemanticSlice StateChange => _application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);

    static SemanticSlice ReplaceSpecification(SemanticSlice slice, SemanticSpecification replacement) =>
        slice with { Specifications = [.. slice.Specifications.Select(value => value.Id == replacement.Id ? replacement : value)] };
}
