// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

// The contract carries what the language cannot declare yet - several events, a composite key, releasing events,
// ignored casing and a message - so it admits them without a change of shape.
public class when_using_valid_constraint_contracts : a_valid_semantic_model
{
    ExecutableSemanticModel _constrained;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        _constrained = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Constraints =
                [
                    new("OneRegistrationPerProject", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(_eventId, [])], [], false, null),
                    new("ProjectNameIsUnique", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(_eventId, [_eventNamePropertyId, _eventProjectIdPropertyId])], [], true, "Project names are unique")
                ]
            }));
    }

    [Fact] void should_carry_both_constraints() => Slice.Constraints.Length.ShouldEqual(2);
    [Fact] void should_keep_the_composite_key_order() => Slice.Constraints.Single(_ => _.Name == "ProjectNameIsUnique").Targets.Single().Properties.ShouldContainOnly([_eventNamePropertyId, _eventProjectIdPropertyId]);
    [Fact] void should_change_the_semantic_revision() => _constrained.Revision.ShouldNotEqual(_model.Revision);

    SemanticSlice Slice => _constrained.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
}
