// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_exposing_source_stream_authoring : given.an_authoring_connection
{
    const string Source = "concept Month : Int\neventsource Account\n  description \"Customer account\"\n  stream Transactions\n    streamId Month\nmodule Banking\n  feature Deposits\n    slice StateChange Deposit\n      command Deposit\n        month Month\n        stream Account.Transactions\n          streamId = month\n";

    [Fact]
    void should_page_source_and_stream_keys_without_enrolling_semantic_ids()
    {
        Start(Source);
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        foreach (var view in new[] { "event-sources", "event-streams", "command-routes" })
        {
            var page = Result("read-workspace", new { expectedRevision = revision, view }).GetProperty("page");
            page.GetProperty("totalCount").GetInt32().ShouldEqual(1);
            page.GetRawText().ShouldNotContain("semanticId");
            page.GetRawText().ShouldNotContain("requirementId");
        }
        Page("event-source-diagnostics", revision).EnumerateArray().Any(diagnostic => diagnostic.GetProperty("code").GetString() == "PLAY0268").ShouldBeTrue();
        var stream = Page("event-streams", revision)[0];
        stream.GetProperty("scope")[0].GetString().ShouldEqual("Account");
        var details = Result("read-workspace", new { expectedRevision = revision, view = "event-stream-details", authoringKey = stream.GetProperty("authoringKey").GetString() });
        details.GetProperty("executionAvailable").GetBoolean().ShouldBeFalse();
        details.GetProperty("executionReadiness").GetString().ShouldContain("Not admitted by any supported executable model (ESM) version yet");
        var source = Node("EventSourceSyntax", revision);
        source.GetProperty("semanticId").ValueKind.ShouldEqual(JsonValueKind.Null);
        source.GetProperty("node").GetProperty("streams")[0].GetProperty("streamId").GetProperty("name").GetString().ShouldEqual("Month");
    }

    [Fact]
    void should_refuse_duplicate_parent_stream_details_without_an_unhandled_sequence_error()
    {
        Start(Source + "eventsource Account\n  stream Other\n");
        var revision = Open().GetProperty("revision").GetString();
        var key = Page("event-streams", revision)[0].GetProperty("authoringKey").GetString();
        var response = Call("read-workspace", new { expectedRevision = revision, view = "event-stream-details", authoringKey = key });
        response.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        response.GetRawText().ShouldContain("AmbiguousDeclaration");
        response.GetRawText().ShouldNotContain("InvalidOperationException");
    }

    [Fact]
    void should_require_catalog_pinning_on_source_inventory_continuation()
    {
        Start(Source + "eventsource Other\n  stream Transactions\n");
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var refused = Call("read-workspace", new { expectedRevision = revision, view = "event-sources", offset = 1, limit = 1 });
        refused.GetRawText().ShouldContain("expectedCatalogRevision");
        var accepted = Result("read-workspace", new { expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), view = "event-sources", offset = 1, limit = 1 });
        accepted.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    }

    [Fact]
    void should_round_trip_typed_route_replacement_preserving_comments_and_unavailability()
    {
        Start(Source.Replace("        month Month", "        // keep the author's intent\n        month Month", StringComparison.Ordinal));
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var route = Node("CommandStreamSyntax", revision);
        var node = JsonNode.Parse(route.GetProperty("node").GetRawText())!;
        node["streamId"]!["source"] = JsonNode.Parse("{\"kind\":\"LiteralExpressionSyntax\",\"value\":7}");
        var proposal = Result("propose-ast", new
        {
            expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = route.GetProperty("handle"), node } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        Candidate(proposal).Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
        Apply(opened, proposal);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("// keep the author's intent");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("streamId = 7");
    }

    [Fact]
    void should_index_and_edit_a_native_scoped_command_using_the_complete_source_catalog()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import \"sources.play\"\nmodule Banking\n  feature Deposits\n    import \"command.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "sources.play"), "concept Month : Int\neventsource Account\n  stream Transactions\n    streamId Month\n");
        File.WriteAllText(Path.Combine(RootPath, "command.play"), "slice StateChange Deposit\n  command Deposit\n    month Month\n    stream Account.Transactions // keep authored route\n      streamId = month\n");
        Initialize();
        var opened = Open();
        var route = Node("CommandStreamSyntax", opened.GetProperty("revision").GetString());
        route.GetProperty("location").GetProperty("path").GetString().ShouldEqual("command.play");
        var node = JsonNode.Parse(route.GetProperty("node").GetRawText())!;
        node["streamId"]!["source"] = JsonNode.Parse("{\"kind\":\"LiteralExpressionSyntax\",\"value\":7}");
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = route.GetProperty("handle"), node } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        Apply(opened, proposal);
        File.ReadAllText(Path.Combine(RootPath, "command.play")).ShouldContain("streamId = 7");
        File.ReadAllText(Path.Combine(RootPath, "command.play")).ShouldContain("// keep authored route");
    }

    [Fact]
    void should_add_replace_remove_source_declarations_through_their_actual_typed_slots()
    {
        Start(Source);
        var opened = Open();
        var application = Node("ApplicationSyntax", opened.GetProperty("revision").GetString());
        var added = Edit(opened, new
        {
            operation = "add", parent = application.GetProperty("handle"), member = "eventSources",
            node = new { kind = "EventSourceSyntax", name = "Additional", streams = new[] { new { kind = "EventStreamSyntax", name = "Ledger" } } }
        });
        opened = Apply(opened, added).GetProperty("workspace");
        var stream = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind = "EventStreamSyntax", name = "Ledger", includeContent = true }).GetProperty("page").GetProperty("items")[0];
        var replaced = Edit(opened, new { operation = "replace", target = stream.GetProperty("handle"), node = new { kind = "EventStreamSyntax", name = "Ledger", description = "Additional ledger" } });
        opened = Apply(opened, replaced).GetProperty("workspace");
        var source = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind = "EventSourceSyntax", name = "Additional", includeContent = true }).GetProperty("page").GetProperty("items")[0];
        var removed = Edit(opened, new { operation = "remove", target = source.GetProperty("handle") });
        Apply(opened, removed).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldNotContain("Additional");
    }

    [Fact]
    void should_not_silently_reinterpret_an_untouched_route_even_in_draft_validation()
    {
        var source = Source.Replace("    streamId Month\n", string.Empty, StringComparison.Ordinal).Replace("          streamId = month\n", string.Empty, StringComparison.Ordinal);
        Start(source);
        var opened = Open();
        var declaration = Node("EventSourceSyntax", opened.GetProperty("revision").GetString());
        var node = JsonNode.Parse(declaration.GetProperty("node").GetRawText())!;
        node["name"] = "Other";
        var refused = Call("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", referencePolicy = "Draft", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = declaration.GetProperty("handle"), node } }
        });
        refused.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        refused.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("ProposalRejected");
        var exported = Result("export-workspace", new { expectedRevision = opened.GetProperty("revision").GetString() });
        var workspace = ScreenplayWorkspaceSerializer.Deserialize(exported.GetProperty("bytesBase64").GetBytesFromBase64());
        var bindings = new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(workspace));
        bindings.Bindings.Count(binding => (binding.Reference.Domain is WorkspaceReferenceDomain.EventSource or WorkspaceReferenceDomain.EventStream) && binding.Target is not null).ShouldEqual(2);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }

    JsonElement Edit(JsonElement opened, object operation) => Result("propose-ast", new
    {
        expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        validation = "Authoring", formatting = "CanonicalizeTouchedDocuments", operations = new[] { operation }
    });

    [Fact]
    void should_inspect_invalid_draft_handles_and_refuse_strict_content_without_unhandled_errors()
    {
        Start("eventsource Account\n  identifier Uuid optional\n");
        var revision = Open().GetProperty("revision").GetString();
        var compact = Result("read-ast", new { expectedRevision = revision, kind = "EventSourceSyntax" });
        compact.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(1);
        var refused = Call("read-ast", new { expectedRevision = revision, kind = "EventSourceSyntax", includeContent = true });
        refused.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        refused.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("InvalidSyntaxJson");
        var exported = Call("merged-document", new { view = "syntax" });
        exported.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        exported.GetRawText().ShouldContain("InvalidSyntaxJson");
        Result("export-workspace", new { expectedRevision = revision }).GetProperty("bytesBase64").GetString().ShouldNotBeEmpty();
    }

    [Fact]
    void should_select_the_retained_property_candidate_through_a_typed_command_edit()
    {
        Start("import Account.Transactions\ntype Transactions\n  value String\n" + Source.Replace("          streamId = month\n", string.Empty, StringComparison.Ordinal));
        var opened = Open();
        var command = Node("CommandSyntax", opened.GetProperty("revision").GetString());
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var candidates = node["streamCandidates"]!.AsArray();
        candidates.Count.ShouldEqual(1);
        node["properties"]!.AsArray().Add(candidates[0]!["propertyCandidate"]!.DeepClone());
        node["streamCandidates"] = new JsonArray();
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", referencePolicy = "Draft", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = command.GetProperty("handle"), node } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        Apply(opened, proposal);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("@stream Account.Transactions");
    }

    [Fact]
    void should_select_the_exact_retained_route_candidate_with_an_explicit_competing_import_removal()
    {
        Start("import Account.Transactions\ntype Transactions\n  value String\n" + Source.Replace("    streamId Month\n", string.Empty, StringComparison.Ordinal).Replace("          streamId = month\n", string.Empty, StringComparison.Ordinal));
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var command = Node("CommandSyntax", revision);
        var import = Node("ImportSyntax", revision);
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var selected = node["streamCandidates"]![0]!.DeepClone();
        selected["propertyCandidate"] = null;
        node["stream"] = selected;
        node["streamCandidates"] = new JsonArray();
        var proposal = Result("propose-ast", new
        {
            expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new object[] { new { operation = "replace", target = command.GetProperty("handle"), node }, new { operation = "remove", target = import.GetProperty("handle") } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        Candidate(proposal).Documents.Single().Text.ShouldNotContain("import Account.Transactions");
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("stream Account.Transactions");
    }

    [Fact]
    void should_select_a_keyed_candidate_only_with_the_explicit_required_mapping()
    {
        Start("import Account.Transactions\ntype Transactions\n  value String\n" + Source.Replace("          streamId = month\n", string.Empty, StringComparison.Ordinal));
        var opened = Open();
        var command = Node("CommandSyntax", opened.GetProperty("revision").GetString());
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var selected = node["streamCandidates"]![0]!.DeepClone();
        selected["propertyCandidate"] = null;
        selected["streamId"] = JsonNode.Parse("{\"kind\":\"PropertyMappingSyntax\",\"property\":\"streamId\",\"source\":{\"kind\":\"PathExpressionSyntax\",\"path\":\"month\"}}");
        node["stream"] = selected;
        node["streamCandidates"] = new JsonArray();
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new object[] { new { operation = "replace", target = command.GetProperty("handle"), node }, new { operation = "remove", target = Node("ImportSyntax", opened.GetProperty("revision").GetString()).GetProperty("handle") } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("streamId = month");
    }

    void Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
    }
}
