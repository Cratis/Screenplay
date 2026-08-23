// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_property_identities_for_different_owner_kinds : Specification
{
    SemanticAddress _commandProperty;
    SemanticAddress _eventProperty;
    SemanticAddress _readModelProperty;
    SemanticId _commandPropertyId;
    SemanticId _eventPropertyId;
    SemanticId _readModelPropertyId;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        _commandProperty = SemanticAddress.ForProperty(SemanticAddress.ForCommand(slice, "Contract"), "Id");
        _eventProperty = SemanticAddress.ForProperty(SemanticAddress.ForEventContract(slice, "Contract"), "Id");
        _readModelProperty = SemanticAddress.ForProperty(SemanticAddress.ForReadModel(slice, "Contract"), "Id");
        _commandPropertyId = SemanticId.Create(_commandProperty);
        _eventPropertyId = SemanticId.Create(_eventProperty);
        _readModelPropertyId = SemanticId.Create(_readModelProperty);
    }

    [Fact] void should_retain_the_command_owner_kind() => _commandProperty.OwnerKind.ShouldEqual(SemanticKind.Command);
    [Fact] void should_retain_the_event_owner_kind() => _eventProperty.OwnerKind.ShouldEqual(SemanticKind.EventContract);
    [Fact] void should_retain_the_read_model_owner_kind() => _readModelProperty.OwnerKind.ShouldEqual(SemanticKind.ReadModel);
    [Fact] void should_not_collide_command_and_event_properties() => _commandPropertyId.ShouldNotEqual(_eventPropertyId);
    [Fact] void should_not_collide_command_and_read_model_properties() => _commandPropertyId.ShouldNotEqual(_readModelPropertyId);
    [Fact] void should_not_collide_event_and_read_model_properties() => _eventPropertyId.ShouldNotEqual(_readModelPropertyId);
}
