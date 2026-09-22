// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Tool.Mcp.for_McpAuthoringWorkflow;

public class when_rejecting_stale_handles_and_incorrect_ast_expectations : given.an_authoring_connection
{
    JsonElement _incorrect;
    JsonElement _stale;
    JsonElement _filtered;
    JsonElement _absent;
    bool _unchangedAfterIncorrect;
    bool _unchangedAfterStale;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var command = Node("CommandSyntax", revision);
        var handle = command.GetProperty("handle");
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText());
        node["description"] = "A different expected command";
        _filtered = Result("read-ast", new
        {
            expectedRevision = revision,
            kind = "CommandSyntax",
            name = "RegisterProject",
            semanticId = command.GetProperty("semanticId").GetString(),
            documentId = handle.GetProperty("documentId").GetString(),
            path = handle.GetProperty("path").GetString()
        }).GetProperty("page");
        _absent = Result("read-ast", new { expectedRevision = revision, kind = "CommandSyntax", name = "MissingCommand" }).GetProperty("page");
        _incorrect = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = handle, expected = node, node = command.GetProperty("node") } }
        });
        _unchangedAfterIncorrect = File.ReadAllText(Path.Combine(RootPath, "application.play")) == Source;
        var proposal = Result("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = handle, expected = command.GetProperty("node"), node } }
        });
        var applied = Apply(opened, proposal).GetProperty("workspace");
        var bytes = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        _stale = Call("propose-ast", new
        {
            expectedRevision = applied.GetProperty("revision").GetString(),
            expectedCatalogRevision = applied.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = handle, expected = node, node = command.GetProperty("node") } }
        });
        _unchangedAfterStale = File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(bytes);
    }

    [Fact] void should_match_all_five_ast_filters_together() => _filtered.GetProperty("totalCount").GetInt32().ShouldEqual(1);
    [Fact] void should_keep_filtered_nodes_compact_by_default() => _filtered.GetProperty("items")[0].GetProperty("node").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_ignore_a_nonmatching_name_filter() => _absent.GetProperty("totalCount").GetInt32().ShouldEqual(0);
    [Fact] void should_reject_the_incorrect_expected_ast() => IsRejected(_incorrect).ShouldBeTrue();
    [Fact] void should_leave_disk_unchanged_after_the_incorrect_expectation() => _unchangedAfterIncorrect.ShouldBeTrue();
    [Fact] void should_reject_an_old_handle_under_the_current_revision() => IsRejected(_stale).ShouldBeTrue();
    [Fact] void should_leave_disk_unchanged_after_the_stale_handle() => _unchangedAfterStale.ShouldBeTrue();

    static bool IsRejected(JsonElement response) => response.TryGetProperty("error", out _) || response.GetProperty("result").GetProperty("isError").GetBoolean();
}
