// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_constraint_contracts : a_valid_semantic_model
{
    SemanticConstraint _uniqueValue;
    SemanticConstraint _uniqueEvent;
    Exception _unresolvedEvent;
    Exception _foreignProperty;
    Exception _propertiesOnUniqueEvent;
    Exception _noPropertiesOnUniqueValue;
    Exception _noTargets;
    Exception _defaultTargets;
    Exception _repeatedTarget;
    Exception _repeatedProperty;
    Exception _duplicateNameAcrossSlices;
    Exception _unknownKind;
    Exception _unknownScope;
    Exception _unresolvedRelease;
    Exception _releasingATarget;
    Exception _ignoringCasingOfAnEvent;
    Exception _emptyMessage;

    void Establish()
    {
        _uniqueValue = new("ProjectNameIsUnique", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(_eventId, [_eventNamePropertyId])], [], false, null);
        _uniqueEvent = new("OneRegistrationPerProject", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(_eventId, [])], [], false, null);
    }

    void Because()
    {
        _unresolvedEvent = Validate(_uniqueEvent with { Targets = [new(_commandId, [])] });
        _foreignProperty = Validate(_uniqueValue with { Targets = [new(_eventId, [_commandNamePropertyId])] });
        _propertiesOnUniqueEvent = Validate(_uniqueEvent with { Targets = [new(_eventId, [_eventNamePropertyId])] });
        _noPropertiesOnUniqueValue = Validate(_uniqueValue with { Targets = [new(_eventId, [])] });
        _noTargets = Validate(_uniqueEvent with { Targets = [] });
        _defaultTargets = Validate(_uniqueEvent with { Targets = default });
        _repeatedTarget = Validate(_uniqueEvent with { Targets = [new(_eventId, []), new(_eventId, [])] });
        _repeatedProperty = Validate(_uniqueValue with { Targets = [new(_eventId, [_eventNamePropertyId, _eventNamePropertyId])] });
        _unknownKind = Validate(_uniqueEvent with { Kind = SemanticConstraintKind.Unknown });
        _unknownScope = Validate(_uniqueEvent with { Scope = SemanticConstraintScope.Unknown });
        _unresolvedRelease = Validate(_uniqueEvent with { ReleasedBy = [_commandId] });
        _releasingATarget = Validate(_uniqueEvent with { ReleasedBy = [_eventId] });
        _ignoringCasingOfAnEvent = Validate(_uniqueEvent with { IgnoreCasing = true });
        _emptyMessage = Validate(_uniqueEvent with { Message = string.Empty });

        var slices = _application.Modules.Single().Features.Single().Slices;
        _duplicateNameAcrossSlices = Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlices([.. slices.Select(slice => slice with { Constraints = [_uniqueEvent] })])));
    }

    [Fact] void should_reject_an_unresolved_event() => _unresolvedEvent.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_property_the_event_does_not_declare() => _foreignProperty.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_properties_on_a_unique_event() => _propertiesOnUniqueEvent.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_unique_value_without_properties() => _noPropertiesOnUniqueValue.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_constraint_without_events() => _noTargets.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_default_targets() => _defaultTargets.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_event_constrained_twice() => _repeatedTarget.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_property_named_twice() => _repeatedProperty.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_name_used_in_two_slices() => _duplicateNameAcrossSlices.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unknown_kind() => _unknownKind.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unknown_scope() => _unknownScope.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unresolved_releasing_event() => _unresolvedRelease.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_event_that_both_claims_and_releases() => _releasingATarget.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_ignoring_casing_of_an_event_occurrence() => _ignoringCasingOfAnEvent.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_empty_message() => _emptyMessage.ShouldBeOfExactType<InvalidSemanticContract>();

    Exception Validate(SemanticConstraint constraint)
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        return Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Constraints = [constraint] })));
    }
}
