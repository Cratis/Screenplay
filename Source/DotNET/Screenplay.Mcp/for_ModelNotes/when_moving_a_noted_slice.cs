// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_moving_a_noted_slice : given.a_notes_workspace
{
    JsonElement _proposal;
    ModelNotesItem _item = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), $"{Source}\n  feature Planning\n    slice StateChange Placeholder\n      event PlaceholderRecorded\n        value String\n");
        Original = Workspace();
        OriginalState = McpState.Serialize(Original);
        File.WriteAllBytes(Files.PathFor(McpState.FileName), OriginalState);
        Opened = Result(Workspaces.Open(McpJson.Empty));
    }

    void Because()
    {
        var slice = Original.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice && assignment.Address.Name == "RegisterProject");
        var parent = Original.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Feature && assignment.Address.Name == "Planning");
        _proposal = Result(Workspaces.Move(Arguments(new
        {
            expectedRevision = Original.Revision.ToString(),
            expectedCatalogRevision = Original.IdentityCatalog.Revision.ToString(),
            target = McpSemanticAddresses.Describe(slice.Address),
            newParent = McpSemanticAddresses.Describe(parent.Address),
            formatting = "CanonicalizeTouchedDocuments"
        })));
        Assert.True(_proposal.GetProperty("success").GetBoolean(), _proposal.GetRawText());
        _ = Apply(_proposal);
        var after = McpState.Deserialize(Files.Read(McpState.FileName)!).Open(Root);
        _item = ModelNotesAnchors.Items(Notes, after.IdentityCatalog).Single();
    }

    [Fact] void should_report_no_orphans() => _proposal.GetProperty("orphanedNotes").GetArrayLength().ShouldEqual(0);
    [Fact] void should_preserve_sidecar_bytes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    [Fact] void should_follow_the_same_identity() => _item.State.ShouldEqual("open");
    [Fact] void should_report_the_new_parent() => _item.Address!.Parts.Any(part => part.Key == "Planning").ShouldBeTrue();
}
