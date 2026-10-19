// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_drafting_a_generation_without_an_identifier_copy;

public class and_the_copy_is_removed : given.an_inline_identifier_copy
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _applied;
    ScreenplayWorkspace _after;
    byte[] _reviewed;

    void Because()
    {
        _proposal = Result("propose-property", Arguments());
        _after = Candidate(_proposal);
        var id = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId = id, view = "semantic-diff" });
        _reviewed = Result("read-proposal", new { proposalId = id, view = "after", documentId = Copy.GetProperty("handle").GetProperty("documentId").GetString() }).GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        _applied = Apply(Opened, _proposal);
    }

    [Fact] void should_match_the_result_json_snapshot() => for_McpAuthoringWorkflow.given.property_result_snapshots.Verify("identifier-copy-evolution", _proposal.GetProperty("property"));
    [Fact] void should_keep_the_report_in_retained_semantic_review() => _review.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_report_an_evolution_draft() => _proposal.GetProperty("property").GetProperty("evolutionDraft").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_extracted_event() => _proposal.GetProperty("property").GetProperty("extractedInlineEvents").GetArrayLength().ShouldEqual(1);
    [Fact] void should_keep_the_original_generation() => Names(_after, 1).ShouldEqual("projectId,name");
    [Fact] void should_draft_the_next_generation_without_the_copy() => Names(_after, 2).ShouldEqual("name");
    [Fact] void should_clear_the_identifier_copy_finding() => _after.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload).ShouldBeFalse();
    [Fact] void should_apply_successfully() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_install_the_exact_reviewed_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_reviewed);
    [Fact] void should_append_the_next_generation() => Encoding.UTF8.GetString(_reviewed).ShouldContain("event Renamed generation 2");

    static string Names(ScreenplayWorkspace workspace, uint generation) => string.Join(",", WorkspaceSyntaxIndex.Create(workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>()
        .Single(@event => @event.Name == "Renamed" && @event.Generation == generation).Properties.Select(property => property.Name));
}
