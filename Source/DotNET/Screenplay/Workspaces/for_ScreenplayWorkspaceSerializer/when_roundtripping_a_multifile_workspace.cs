// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_roundtripping_a_multifile_workspace : given.a_transport_workspace
{
    byte[] _bytes = null!;
    ScreenplayWorkspace _result = null!;

    void Because()
    {
        _bytes = ScreenplayWorkspaceSerializer.Serialize(Workspace);
        _result = ScreenplayWorkspaceSerializer.Deserialize(_bytes);
    }

    [Fact] void should_preserve_the_revision() => _result.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_preserve_the_friendly_name() => _result.ApplicationName.ShouldEqual(Workspace.ApplicationName);
    [Fact] void should_preserve_the_independent_application_identity() => _result.IdentityCatalog.Application.ShouldEqual(StableApplicationIdentity);
    [Fact] void should_use_non_bootstrap_document_identities() => _result.Documents.All(document => document.Id != DocumentId.Create(document.StableKey)).ShouldBeTrue();
    [Fact] void should_preserve_document_identity_keys_and_paths() => _result.Documents.Select(document => (document.Id, document.StableKey, document.Path)).SequenceEqual(Workspace.Documents.Select(document => (document.Id, document.StableKey, document.Path))).ShouldBeTrue();
    [Fact] void should_preserve_every_source_byte() => _result.Documents.Zip(Workspace.Documents).All(pair => pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan())).ShouldBeTrue();
    [Fact] void should_preserve_the_bom() => _result.Documents.Single(document => document.Id == Concepts.Id).Encoding.ShouldEqual(WorkspaceTextEncoding.Utf8WithBom);
    [Fact] void should_preserve_the_exact_catalog() => SemanticIdentityCatalogSerializer.Serialize(_result.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(Workspace.IdentityCatalog)).ShouldBeTrue();
    [Fact] void should_compile_successfully() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_semantic_output() => SemanticModelSerializer.Serialize(_result.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model)).ShouldBeTrue();
    [Fact] void should_serialize_deterministically() => ScreenplayWorkspaceSerializer.Serialize(Workspace).SequenceEqual(_bytes).ShouldBeTrue();
    [Fact] void should_roundtrip_canonical_bytes() => ScreenplayWorkspaceSerializer.Serialize(_result).SequenceEqual(_bytes).ShouldBeTrue();
}
