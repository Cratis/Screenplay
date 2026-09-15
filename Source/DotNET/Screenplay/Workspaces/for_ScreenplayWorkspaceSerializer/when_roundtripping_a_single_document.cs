// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_roundtripping_a_single_document : given.a_transport_workspace
{
    ScreenplayWorkspace _original = null!;
    ScreenplayWorkspace _result = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create(
            DocumentId.Create("persisted-single-document-42"),
            "application",
            PortablePlayPath.Parse("Application.play"),
            Encoding.UTF8.GetBytes($"{ConceptsSource}\n{RegistrationSource}\n"));
        var catalog = SemanticIdentityCatalog.Create(
            StableApplicationIdentity,
            [new(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)],
            Workspace.IdentityCatalog.Semantics,
            Workspace.IdentityCatalog.EventContracts);
        _original = ScreenplayWorkspace.Create(StableApplicationIdentity, Workspace.ApplicationName, [document], catalog);
    }

    void Because() => _result = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(_original));

    [Fact] void should_preserve_the_single_document_identity() => _result.Documents.Single().Id.ShouldEqual(_original.Documents.Single().Id);
    [Fact] void should_preserve_exact_bytes_without_a_bom() => _result.Documents.Single().Bytes.AsSpan().SequenceEqual(_original.Documents.Single().Bytes.AsSpan()).ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _result.Revision.ShouldEqual(_original.Revision);
    [Fact] void should_compile_successfully() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_semantics_of_the_multifile_form() => SemanticModelSerializer.Serialize(_result.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model)).ShouldBeTrue();
}
