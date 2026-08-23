// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_structurally_equal_composite_keys : a_valid_semantic_model
{
    Exception _accepted;
    Exception _duplicateGivenState;
    Exception _duplicateQueryExpectation;

    void Because()
    {
        var stateChange = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
        var stateView = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        var success = stateChange.Specifications.Single(_ => _.ThenEvents.Length > 0);
        var keyTypeId = Id(SemanticKind.CompositeType, "ProjectKey");
        var keyValuePropertyId = Id(SemanticKind.Property, "ProjectKey.Value");
        var keyType = new SemanticCompositeType(
            keyTypeId,
            "ProjectKey",
            [new(keyValuePropertyId, "Value", SemanticTypeReference.ForConcept(_projectIdConceptId), false)]);
        var keyReference = SemanticTypeReference.ForCompositeType(keyTypeId);
        var firstKey = SemanticValue.Composite(
        [
            new(keyValuePropertyId, SemanticValue.Text("00000000-0000-0000-0000-000000000001"))
        ]);
        var secondKey = SemanticValue.Composite(
        [
            new(keyValuePropertyId, SemanticValue.Text("00000000-0000-0000-0000-000000000001"))
        ]);

        var eventContract = stateChange.Events.Single() with
        {
            Properties = [.. stateChange.Events.Single().Properties.Select(_ =>
                _.Id == _eventProjectIdPropertyId ? _ with { Type = keyReference } : _)]
        };
        var command = stateChange.Commands.Single() with
        {
            Properties = [.. stateChange.Commands.Single().Properties.Select(_ =>
                _.Id == _commandProjectIdPropertyId ? _ with { Type = keyReference } : _)]
        };
        var readModel = stateView.ReadModels.Single() with
        {
            Properties = [.. stateView.ReadModels.Single().Properties.Select(_ =>
                _.Id == _readModelProjectIdPropertyId ? _ with { Type = keyReference } : _)]
        };
        var query = stateView.Queries.Single() with
        {
            Argument = stateView.Queries.Single().Argument with { Type = keyReference }
        };
        var commandValues = success.When.Values.Select(_ =>
            _.TargetProperty == _commandProjectIdPropertyId ? _ with { Value = firstKey } : _).ToImmutableArray();
        var expectedEvent = success.ThenEvents.Single() with
        {
            Values = [.. success.ThenEvents.Single().Values.Select(_ =>
                _.TargetProperty == _eventProjectIdPropertyId ? _ with { Value = firstKey } : _)]
        };
        var state = success.ThenReadModels.Single() with
        {
            Key = firstKey,
            Values = [.. success.ThenReadModels.Single().Values.Select(_ =>
                _.TargetProperty == _readModelProjectIdPropertyId ? _ with { Value = secondKey } : _)]
        };
        var queryExpectation = success.ThenQueries.Single() with
        {
            Key = secondKey,
            Results = [state]
        };
        var typedSuccess = success with
        {
            When = success.When with { Values = commandValues },
            ThenEvents = [expectedEvent],
            ThenReadModels = [state],
            ThenQueries = [queryExpectation]
        };
        var typedStateChange = stateChange with
        {
            Events = [eventContract],
            Commands = [command],
            Specifications = [typedSuccess]
        };
        var typedStateView = stateView with
        {
            ReadModels = [readModel],
            Queries = [query]
        };

        _accepted = Validate(typedSuccess);
        var conflictingState = state with
        {
            Key = secondKey,
            Values = [.. state.Values.Select(_ => _.TargetProperty == _readModelNamePropertyId
                ? _ with { Value = SemanticValue.Text("Conflicting") }
                : _)]
        };
        _duplicateGivenState = Validate(typedSuccess with { GivenReadModels = [state, conflictingState] });
        _duplicateQueryExpectation = Validate(typedSuccess with
        {
            ThenQueries = [queryExpectation, queryExpectation with { Key = firstKey, Results = [] }]
        });

        Exception Validate(SemanticSpecification specification)
        {
            var application = ReplaceSlices(
                typedStateChange with { Specifications = [specification] },
                typedStateView);
            application = application with { Types = [.. application.Types, keyType] };
            return Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application));
        }
    }

    [Fact] void should_accept_structurally_equal_state_and_query_keys() => _accepted.ShouldBeNull();
    [Fact] void should_reject_structurally_duplicate_given_state_keys() => _duplicateGivenState.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_structurally_duplicate_query_expectation_keys() => _duplicateQueryExpectation.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
