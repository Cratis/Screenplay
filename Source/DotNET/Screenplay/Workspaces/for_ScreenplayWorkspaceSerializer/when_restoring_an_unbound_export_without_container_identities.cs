// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_restoring_an_unbound_export_without_container_identities : for_ScreenplayWorkspace.given.an_unbound_container_workspace
{
    ScreenplayWorkspace _legacy = null!;
    ScreenplayWorkspace _restored = null!;
    byte[] _export = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Create(Application, Workspace.IdentityCatalog.Documents, [], []);
        _legacy = ScreenplayWorkspace.CreateValidated(Workspace.ApplicationName, Workspace.Documents, catalog, Workspace.Compilation);
        _export = ScreenplayWorkspaceSerializer.Serialize(_legacy);
    }

    void Because() => _restored = ScreenplayWorkspaceSerializer.Deserialize(_export);

    [Fact] void should_retain_the_binding_failure() => _restored.Compilation.Success.ShouldBeFalse();
    [Fact] void should_add_exactly_the_deterministic_container_assignments() => SemanticIdentityCatalogSerializer.Serialize(_restored.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(Workspace.IdentityCatalog)).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_source_bytes() => _restored.Documents.Single().Bytes.AsSpan().SequenceEqual(_legacy.Documents.Single().Bytes.AsSpan()).ShouldBeTrue();
    [Fact] void should_return_the_materialized_revision() => _restored.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_advance_the_legacy_revision() => _restored.Revision.ShouldNotEqual(_legacy.Revision);
    [Fact] void should_roundtrip_the_new_export_without_changes() => ScreenplayWorkspaceSerializer.Serialize(ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(_restored))).SequenceEqual(ScreenplayWorkspaceSerializer.Serialize(_restored)).ShouldBeTrue();
}
