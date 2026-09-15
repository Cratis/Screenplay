// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_relocating_and_reordering_documents : given.a_transport_workspace
{
    ScreenplayWorkspace _reordered = null!;
    ScreenplayWorkspace _relocated = null!;

    void Because()
    {
        _reordered = Roundtrip(ScreenplayWorkspace.Create(
            StableApplicationIdentity,
            Workspace.ApplicationName,
            [.. Workspace.Documents.Reverse()],
            Workspace.IdentityCatalog));
        _relocated = Roundtrip(ScreenplayWorkspace.Create(
            StableApplicationIdentity,
            Workspace.ApplicationName,
            [.. Workspace.Documents.Reverse().Select(document => WorkspaceDocument.Create(
                document.Id,
                document.StableKey,
                PortablePlayPath.Parse($"relocated/{document.Path.Value}"),
                document.Bytes.AsSpan()))],
            Workspace.IdentityCatalog));
    }

    [Fact] void should_ignore_input_document_order() => _reordered.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_serialize_reordered_input_identically() => ScreenplayWorkspaceSerializer.Serialize(_reordered).SequenceEqual(ScreenplayWorkspaceSerializer.Serialize(Workspace)).ShouldBeTrue();
    [Fact] void should_change_the_exact_workspace_revision_on_relocation() => _relocated.Revision.ShouldNotEqual(Workspace.Revision);
    [Fact] void should_preserve_identities_on_relocation() => SemanticIdentityCatalogSerializer.Serialize(_relocated.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(Workspace.IdentityCatalog)).ShouldBeTrue();
    [Fact] void should_preserve_semantic_output_on_relocation() => SemanticModelSerializer.Serialize(_relocated.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Workspace.Compilation.Value!.Model)).ShouldBeTrue();

    static ScreenplayWorkspace Roundtrip(ScreenplayWorkspace workspace) =>
        ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(workspace));
}
