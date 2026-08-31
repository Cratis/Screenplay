// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_external_identity_assignments : a_workspace_with_external_identities
{
    byte[] _json = [];
    byte[] _reserialized = [];
    ExecutableSemanticModel _roundTripped = null!;

    void Because()
    {
        _json = SemanticModelSerializer.Serialize(Workspace.Compilation.Value.Model);
        _roundTripped = SemanticModelSerializer.Deserialize(_json);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
    }

    SemanticSlice Slice => _roundTripped.Application.Modules.Single().Features.Single().Slices.Single();

    [Fact] void should_preserve_the_external_application_semantic_identity() => _roundTripped.Application.Id.ShouldEqual(ApplicationSemanticIdentity);
    [Fact] void should_preserve_the_external_command_semantic_identity() => Slice.Commands.Single().Id.ShouldEqual(CommandSemanticIdentity);
    [Fact] void should_preserve_the_external_event_semantic_identity() => Slice.Events.Single().Id.ShouldEqual(EventSemanticIdentity);
    [Fact] void should_preserve_the_external_event_contract_identity() => Slice.Events.Single().ContractId.ShouldEqual(EventContractIdentity);
    [Fact] void should_preserve_the_semantic_revision() => _roundTripped.Revision.ShouldEqual(Workspace.Compilation.Value.Model.Revision);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();
}
