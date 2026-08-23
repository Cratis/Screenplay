// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_incoherent_specifications : a_valid_semantic_model
{
    Exception _duplicateCommandTarget;
    Exception _inconsistentStateKey;
    Exception _mixedOutcome;
    Exception _inexactCommand;
    Exception _unproducedEvent;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Specifications.Length > 0);
        var success = slice.Specifications.Single(_ => _.ThenEvents.Length > 0);
        _mixedOutcome = Validate(success with { ThenErrors = [new(null, null)] });
        _inexactCommand = Validate(success with { When = success.When with { Values = [success.When.Values[0]] } });
        _duplicateCommandTarget = Validate(success with
        {
            When = success.When with { Values = [success.When.Values[0], success.When.Values[0]] }
        });
        var state = success.ThenReadModels.Single();
        var inconsistentState = state with
        {
            Values = state.Values.SetItem(0, state.Values[0] with
            {
                Value = SemanticValue.Text("00000000-0000-0000-0000-000000000002")
            })
        };
        _inconsistentStateKey = Validate(success with { ThenReadModels = [inconsistentState] });

        var otherEventId = Id(SemanticKind.EventContract, "ProjectArchived");
        var otherProjectIdProperty = Id(SemanticKind.Property, "ProjectArchived.ProjectId");
        var otherEvent = new SemanticEventContract(
            otherEventId,
            EventContractId.CreateLegacy(_applicationIdentity, "ProjectArchived"),
            EventContractRevision.Initial,
            "ProjectArchived",
            [new(otherProjectIdProperty, "ProjectId", SemanticTypeReference.ForConcept(_projectIdConceptId), true)]);
        var unexpected = new SemanticSpecificationEvent(
            otherEventId,
            [new(otherProjectIdProperty, success.When.Values[0].Value)]);
        var unexpectedSpecification = success with { ThenEvents = [unexpected] };
        _unproducedEvent = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Events = [.. slice.Events, otherEvent],
                Specifications = [.. slice.Specifications.Select(_ => _.Id == success.Id ? unexpectedSpecification : _)]
            })));

        Exception Validate(SemanticSpecification replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Specifications = [.. slice.Specifications.Select(_ => _.Id == replacement.Id ? replacement : _)]
            })));
    }

    [Fact] void should_reject_a_duplicate_command_target() => _duplicateCommandTarget.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_inconsistent_state_key() => _inconsistentStateKey.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_mixed_rejection_and_success_outcomes() => _mixedOutcome.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_inexact_command_shape() => _inexactCommand.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_event_the_command_does_not_produce() => _unproducedEvent.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
