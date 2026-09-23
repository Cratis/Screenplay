// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_constraints : a_valid_semantic_model
{
    ExecutableSemanticModel _constrained;
    byte[] _json;
    byte[] _reserialized;
    ExecutableSemanticModel _roundTripped;

    void Establish()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        _constrained = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Constraints =
                [
                    new("ProjectNameIsUnique", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(_eventId, [_eventNamePropertyId, _eventProjectIdPropertyId])], [], true, "Project names are unique"),
                    new("OneRegistrationPerProject", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(_eventId, [])], [], false, null)
                ]
            }));
    }

    void Because()
    {
        _json = SemanticModelSerializer.Serialize(_constrained);
        _roundTripped = SemanticModelSerializer.Deserialize(_json);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_preserve_the_revision() => _roundTripped.Revision.ShouldEqual(_constrained.Revision);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();
    [Fact] void should_order_constraints_by_name() => Constraints.Select(_ => _.Name).ShouldContainOnly(["OneRegistrationPerProject", "ProjectNameIsUnique"]);
    [Fact] void should_preserve_the_unique_value() => Constraints[1].Kind.ShouldEqual(SemanticConstraintKind.UniquePropertyValue);
    [Fact] void should_preserve_ignored_casing() => Constraints[1].IgnoreCasing.ShouldBeTrue();
    [Fact] void should_preserve_the_message() => Constraints[1].Message.ShouldEqual("Project names are unique");
    [Fact] void should_preserve_the_composite_key_order() => Constraints[1].Targets.Single().Properties.SequenceEqual([_eventNamePropertyId, _eventProjectIdPropertyId]).ShouldBeTrue();
    [Fact] void should_preserve_the_unique_event() => Constraints[0].Kind.ShouldEqual(SemanticConstraintKind.UniqueEventOccurrence);
    [Fact] void should_preserve_the_scope() => Constraints.All(_ => _.Scope == SemanticConstraintScope.EventSequence).ShouldBeTrue();
    [Fact] void should_write_every_member_of_a_constraint() => Encoding.UTF8.GetString(_json).ShouldContain("\"releasedBy\":[],\"ignoreCasing\":false,\"message\":null");
    [Fact] void should_omit_the_member_from_a_slice_without_constraints() => Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model)).ShouldNotContain("\"constraints\"");

    SemanticConstraint[] Constraints => [.. _roundTripped.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0).Constraints];
}
