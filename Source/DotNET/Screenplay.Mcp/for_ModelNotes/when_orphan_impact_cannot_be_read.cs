// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_orphan_impact_cannot_be_read : given.a_notes_workspace
{
    JsonElement _proposal;
    JsonElement _review;

    void Establish() => File.WriteAllText(Files.PathFor(ModelNotesStore.FileName), "{");

    void Because()
    {
        var target = WorkspaceSyntaxIndex.Create(Original).Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice);
        _proposal = Result(Workspaces.Rename(Arguments(new
        {
            expectedRevision = Original.Revision.ToString(),
            expectedCatalogRevision = Original.IdentityCatalog.Revision.ToString(),
            target = McpAstHandles.Describe(target.Handle),
            expectedName = "RegisterProject",
            newName = "StartProject"
        })));
        _review = Result(Workspaces.ReadProposal(Arguments(new { proposalId = _proposal.GetProperty("proposalId").GetString() })));
    }

    [Fact] void should_report_unknown_orphans_on_proposal() => _proposal.GetProperty("orphanedNotes").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_unknown_orphans_on_review() => _review.GetProperty("orphanedNotes").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_unreadable_status() => _proposal.GetProperty("notes").GetProperty("status").GetString().ShouldEqual("unreadable");
    [Fact] void should_include_the_reason() => _proposal.GetProperty("notes").GetProperty("reason").GetString().ShouldNotBeNull();
}
