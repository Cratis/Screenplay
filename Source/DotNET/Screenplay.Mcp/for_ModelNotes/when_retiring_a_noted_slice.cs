// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_retiring_a_noted_slice : given.a_notes_workspace
{
    JsonElement _proposal;
    ModelNotesItem _orphan = null!;

    void Because()
    {
        var retired = Original.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Module)).Select(assignment => McpSemanticAddresses.Describe(assignment.Address));
        var arguments = Arguments(new
        {
            expectedRevision = Original.Revision.ToString(),
            expectedCatalogRevision = Original.IdentityCatalog.Revision.ToString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            documents = new[] { new { operation = "replace-document", documentId = Original.Documents[0].Id.ToString(), source = "concept ProjectId : Uuid\nconcept ProjectName : String\n" } },
            retiredSemanticAddresses = retired,
            retiredEventAddresses = Original.IdentityCatalog.EventContracts.Select(assignment => McpSemanticAddresses.Describe(assignment.Address))
        });
        _proposal = Result(Workspaces.ProposeAst(arguments, source: true));
        _ = Apply(_proposal);
        var after = McpState.Deserialize(Files.Read(McpState.FileName)!).Open(Root);
        _orphan = ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), after.IdentityCatalog.Application).Document!, after.IdentityCatalog).Single();
    }

    [Fact] void should_report_orphans_before_apply() => _proposal.GetProperty("orphanedNotes")[0].GetString().ShouldEqual(Notes.Notes[0].Id);
    [Fact] void should_preserve_the_sidecar_bytes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    [Fact] void should_keep_the_orphan() => _orphan.State.ShouldEqual("orphaned");
    [Fact] void should_not_resolve_the_last_address_hint() => _orphan.Address.ShouldBeNull();
}
