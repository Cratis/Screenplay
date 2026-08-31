// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_round_tripping_external_identity_assignments : a_workspace_with_external_identities
{
    byte[] _json = [];
    byte[] _reserialized = [];
    SemanticIdentityCatalog _roundTripped = null!;

    void Because()
    {
        _json = SemanticIdentityCatalogSerializer.Serialize(Workspace.IdentityCatalog);
        _roundTripped = SemanticIdentityCatalogSerializer.Deserialize(_json);
        _reserialized = SemanticIdentityCatalogSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_preserve_the_explicit_application_identity() => _roundTripped.Application.ShouldEqual(StableApplicationIdentity);
    [Fact] void should_preserve_the_external_application_semantic_identity() => _roundTripped.ResolveSemantic(ApplicationAddress).ShouldEqual(ApplicationSemanticIdentity);
    [Fact] void should_preserve_the_external_command_semantic_identity() => _roundTripped.ResolveSemantic(CommandAddress).ShouldEqual(CommandSemanticIdentity);
    [Fact] void should_preserve_the_external_event_semantic_identity() => _roundTripped.ResolveSemantic(EventAddress).ShouldEqual(EventSemanticIdentity);
    [Fact] void should_preserve_the_external_event_contract_identity() => _roundTripped.ResolveEventContract(EventAddress).Id.ShouldEqual(EventContractIdentity);
    [Fact] void should_preserve_the_catalog_revision() => _roundTripped.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();
}
