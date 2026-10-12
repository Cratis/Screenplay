// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_listing_questions : given.a_notes_workspace
{
    JsonElement _items;
    JsonElement _findings;

    void Because()
    {
        _items = Result(Workspaces.ListNotes(Arguments(new { @ref = "D123", scope = "Projects.Registration.RegisterProject" })));
        _findings = Result(Workspaces.ListNotes(Arguments(new { view = "findings" })));
    }

    [Fact] void should_report_the_pinned_revision() => _items.GetProperty("notesRevision").GetString().ShouldEqual(Opened.GetProperty("notesRevision").GetString());
    [Fact] void should_select_references() => _items.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(1);
    [Fact] void should_return_host_only_code() => _findings.GetProperty("page").GetProperty("items")[0].GetProperty("code").GetString().ShouldEqual("model-note.open-question");
    [Fact] void should_return_info_severity() => _findings.GetProperty("page").GetProperty("items")[0].GetProperty("severity").GetString().ShouldEqual("info");
    [Fact] void should_address_the_slice() => _findings.GetProperty("page").GetProperty("items")[0].GetProperty("address").GetProperty("kind").GetString().ShouldEqual("Slice");
    [Fact] void should_require_revision_on_continuation() => ((McpFailure)Catch.Exception(() => Workspaces.ListNotes(Arguments(new { offset = 1 })))).Code.ShouldEqual(-32602);
    [Fact] void should_refuse_a_stale_continuation() => Catch.Exception(() => Workspaces.ListNotes(Arguments(new { offset = 1, expectedNotesRevision = "nrev1:stale" }))).ShouldBeOfExactType<McpFailure>();
}
