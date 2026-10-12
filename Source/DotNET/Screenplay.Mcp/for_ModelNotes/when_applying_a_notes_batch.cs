// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_applying_a_notes_batch : given.a_notes_workspace
{
    JsonElement _result;
    JsonElement _review;
    ScreenplayWorkspace _reopened = null!;
    ModelNotesRead _notes = null!;

    void Because()
    {
        var proposal = ProposeNotes([new { operation = "resolve", id = Notes.Notes[0].Id, resolution = new { by = "external-decision", text = "Cancellation permitted until check-in.", author = "sindre", @ref = new { id = "D123", url = "https://example.org/decisions/D123" } } }, new { operation = "add", kind = "task", text = "Add the refusal specification.", author = "sindre", anchor = new { application = true } }]);
        _review = Result(Workspaces.ReadProposal(Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "notes" })));
        _result = Apply(proposal);
        _reopened = McpState.Deserialize(Files.Read(McpState.FileName)!).Open(Root);
        _notes = ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Original.IdentityCatalog.Application);
    }

    [Fact] void should_apply() => _result.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_review_both_notes() => _review.GetProperty("result").GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    [Fact] void should_preserve_workspace_revision() => _reopened.Revision.ShouldEqual(Original.Revision);
    [Fact] void should_preserve_catalog_revision() => _reopened.IdentityCatalog.Revision.ShouldEqual(Original.IdentityCatalog.Revision);
    [Fact] void should_preserve_semantic_bytes() => SemanticModelSerializer.Serialize(_reopened.Compilation.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(Original.Compilation.Value!.Model)).ShouldBeTrue();
    [Fact] void should_preserve_semantic_revision() => _reopened.Compilation.Value!.Model.Revision.ShouldEqual(Original.Compilation.Value!.Model.Revision);
    [Fact] void should_preserve_source_bytes() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_resolve_by_external_decision() => _notes.Document!.Notes.Single(note => note.Kind == "question").Resolution!.Ref!.Id.ShouldEqual("D123");
    [Fact] void should_clear_the_journal() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
