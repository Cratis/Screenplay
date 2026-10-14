// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_PersistedIdentityStateSerializer.when_deserializing;

public class with_canonical_bytes : given.a_persisted_workspace
{
    PersistedIdentityState _state = null!;

    void Because() => _state = PersistedIdentityStateSerializer.Deserialize(Bytes);

    [Fact] void should_read_the_application_name() => _state.ApplicationName.ShouldEqual("Projects");
    [Fact] void should_read_the_catalog() => SemanticIdentityCatalogSerializer.Serialize(_state.IdentityCatalog).ShouldEqual(SemanticIdentityCatalogSerializer.Serialize(Workspace.IdentityCatalog));
    [Fact] void should_read_the_document_mapping() => _state.Documents.Single().ShouldEqual(new PersistedDocumentMapping(Workspace.Documents[0].Id, "application", PortablePlayPath.Parse("application.play")));
    [Fact] void should_round_trip_the_bytes() => PersistedIdentityStateSerializer.Serialize(_state).ShouldEqual(Bytes);
    [Fact] void should_write_the_wrapper_schema() => System.Text.Encoding.UTF8.GetString(Bytes).ShouldContain("\"schema\":\"cratis.screenplay.mcp-identities\"");
}
