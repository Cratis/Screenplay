// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_errorful_source_streams : given.an_authoring_connection
{
    [Fact]
    void should_retain_duplicate_routes_and_every_candidate_in_the_read_inventory()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions\n        stream Account.Transactions\n");
        Initialize();
        var revision = Open().GetProperty("revision").GetString();
        var routes = Page("command-routes", revision);
        routes.GetArrayLength().ShouldEqual(1);
        routes[0].GetProperty("authoredRoute").GetProperty("stream").GetString().ShouldEqual("Transactions");
        routes[0].GetProperty("ambiguousStreamCandidates").GetArrayLength().ShouldEqual(1);
        Result("read-workspace", new { expectedRevision = revision, view = "command-routes" }).GetProperty("authoringDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
        Result("read-ast", new { expectedRevision = revision, kind = "CommandSyntax" }).GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
    }

    [Theory]
    [InlineData("Other", false)]
    [InlineData("Transactions", false)]
    [InlineData("Other", true)]
    [InlineData("Transactions", true)]
    void should_never_claim_unique_children_when_an_errorful_document_retains_a_second_parent(string stream, bool reverse)
    {
        const string clean = "eventsource Account\n  stream Transactions\n";
        var errorful = $"eventsource Account\n  stream {stream}\n  invalid directive\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), reverse ? errorful : clean);
        File.WriteAllText(Path.Combine(RootPath, "other.play"), reverse ? clean : errorful);
        Initialize();
        var revision = Open().GetProperty("revision").GetString();
        var sources = Page("event-sources", revision);
        sources.GetArrayLength().ShouldEqual(2);
        foreach (var child in Page("event-streams", revision).EnumerateArray()) child.GetProperty("ownership").GetString().ShouldEqual("ambiguous");
        var key = Page("event-streams", revision)[0].GetProperty("authoringKey").GetString();
        var refused = Call("read-workspace", new { expectedRevision = revision, view = "event-stream-details", authoringKey = key });
        refused.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        refused.GetRawText().ShouldContain("AmbiguousDeclaration");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_use_authoritative_barrel_placement_without_making_errorful_nodes_editable(bool reverse)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import \"sources.play\"\nimport \"duplicate.play\"\nmodule M\n  feature F\n    import \"barrel.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "barrel.play"), "import \"command.play\"\n");
        const string clean = "eventsource Account\n  stream Transactions\n";
        const string errorful = "eventsource Account\n  stream Other\n  invalid directive\n";
        File.WriteAllText(Path.Combine(RootPath, "sources.play"), reverse ? errorful : clean);
        File.WriteAllText(Path.Combine(RootPath, "duplicate.play"), reverse ? clean : errorful);
        File.WriteAllText(Path.Combine(RootPath, "command.play"), "slice StateChange S\n  command C\n    stream Account.Transactions\n");
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var routes = Page("command-routes", revision);
        routes[0].GetProperty("scope").GetRawText().ShouldEqual("[\"M\",\"F\",\"S\"]");
        routes[0].GetProperty("authoredRoute").GetProperty("eventSource").GetString().ShouldEqual("Account");
        foreach (var child in Page("event-streams", revision).EnumerateArray()) child.GetProperty("ownership").GetString().ShouldEqual("ambiguous");
        var source = Page("event-sources", revision).EnumerateArray().Single(source => source.GetProperty("location").GetProperty("path").GetString() == (reverse ? "sources.play" : "duplicate.play"));
        var refused = Call("propose-ast", new
        {
            expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "remove", target = source.GetProperty("handle") } }
        });
        (refused.TryGetProperty("error", out _) || refused.GetProperty("result").GetProperty("isError").GetBoolean()).ShouldBeTrue();
        refused.GetRawText().ShouldContain("UnknownNode");
        File.ReadAllText(Path.Combine(RootPath, reverse ? "sources.play" : "duplicate.play")).ShouldEqual(errorful);
    }

    [Fact]
    void should_disclose_incomplete_extent_instead_of_a_false_unique_owner()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "eventsource Account\n  stream Transactions\nunknown block\n  eventsource Account\n    stream Transactions\n");
        Initialize();
        var revision = Open().GetProperty("revision").GetString();
        var page = Result("read-workspace", new { expectedRevision = revision, view = "event-streams" });
        page.GetProperty("inventoryComplete").GetBoolean().ShouldBeFalse();
        page.GetProperty("page").GetProperty("items")[0].GetProperty("ownership").GetString().ShouldEqual("incomplete");
        page.GetProperty("authoringDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
        var diagnostics = Page(page.GetProperty("authoringDiagnosticsView").GetString()!, revision);
        diagnostics.EnumerateArray().Any(diagnostic => diagnostic.GetProperty("location").GetProperty("path").GetString() == "application.play").ShouldBeTrue();
    }
}
