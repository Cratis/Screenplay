// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_renaming_a_noted_slice : given.a_notes_workspace
{
    JsonElement _proposal;
    ModelNotesItem _item = null!;

    void Because()
    {
        var target = WorkspaceSyntaxIndex.Create(Original).Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice);
        _proposal = Result(Workspaces.Rename(Arguments(new
        {
            expectedRevision = Original.Revision.ToString(), expectedCatalogRevision = Original.IdentityCatalog.Revision.ToString(),
            target = McpAstHandles.Describe(target.Handle), expectedName = "RegisterProject", newName = "StartProject"
        })));
        _ = Apply(_proposal);
        var after = McpState.Deserialize(Files.Read(McpState.FileName)!).Open(Root);
        _item = ModelNotesAnchors.Items(Notes, after.IdentityCatalog).Single();
    }

    [Fact] void should_report_no_orphans() => _proposal.GetProperty("orphanedNotes").GetArrayLength().ShouldEqual(0);
    [Fact] void should_preserve_the_sidecar_bytes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    [Fact] void should_follow_the_identity() => _item.Address!.Parts[^1].Key.ShouldEqual("StartProject");
    [Fact] void should_ignore_the_stale_hint() => _item.Note.LastAddress!.Parts[^1].Key.ShouldEqual("RegisterProject");
}
