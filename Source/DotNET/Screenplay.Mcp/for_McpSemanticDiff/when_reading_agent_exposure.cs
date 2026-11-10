// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_reading_agent_exposure : given.a_semantic_comparison
{
    const string Exposure = "agents\n  tokens personal\n  query All\n    scope read\n    description \"Read projects\"\n  query History\n    scope read\n    description \"Read history\"\n  command Register\n    scope write\n    description \"Register projects\"\n";
    JsonElement _full;
    JsonElement _first;
    JsonElement _second;
    JsonElement _repeated;
    JsonElement _incompleteFirst;
    JsonElement _incompleteSecond;
    Exception? _stale;
    Exception? _unpinned;

    void Because()
    {
        CompareSnapshots(Source, Source + "\n" + Exposure);
        _full = Read(new { view = "agent-exposure", version = 2, limit = 200 });
        _first = Read(new { view = "agent-exposure", version = 2, limit = 1 });
        _repeated = Read(new { view = "agent-exposure", version = 2, limit = 1 });
        var revision = _first.GetProperty("sourceRevision").GetString();
        _second = Read(new { view = "agent-exposure", version = 2, offset = 1, limit = 2, expectedSourceRevision = revision });
        _stale = Catch.Exception(() => Read(new { view = "agent-exposure", version = 2, offset = 1, limit = 1, expectedSourceRevision = "stale" }));
        _unpinned = Catch.Exception(() => Read(new { view = "agent-exposure", version = 2, offset = 1, limit = 1 }));
        _incompleteFirst = ReadIncomplete(new { view = "agent-exposure", version = 2, limit = 1 });
        var incompleteRevision = _incompleteFirst.GetProperty("sourceRevision").GetString();
        _incompleteSecond = ReadIncomplete(new { view = "agent-exposure", version = 2, offset = 1, limit = 1, expectedSourceRevision = incompleteRevision });
    }

    [Fact] void should_report_complete_coverage() => _full.GetProperty("coverage").GetString().ShouldEqual("Complete");
    [Fact] void should_report_widening() => _full.GetProperty("hasWidening").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_rows_out_of_the_default_response() => Diff.TryGetProperty("coverage", out _).ShouldBeFalse();
    [Fact] void should_add_exactly_one_default_limit() => Diff.GetProperty("limits").EnumerateArray().Count(limit => limit.GetString() == "Agent exposure was not compared; use the agent-exposure view.").ShouldEqual(1);
    [Fact] void should_return_all_exposed_operations() => _full.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(3);
    [Fact] void should_classify_every_new_operation_as_added() => _full.GetProperty("page").GetProperty("items").EnumerateArray().All(item => item.GetProperty("change").GetString() == "Added").ShouldBeTrue();
    [Fact] void should_return_byte_identical_repeated_pages() => _repeated.GetRawText().ShouldEqual(_first.GetRawText());
    [Fact] void should_advance_the_offset() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_reassemble_the_same_ordered_rows() => _first.GetProperty("page").GetProperty("items").EnumerateArray().Concat(_second.GetProperty("page").GetProperty("items").EnumerateArray()).Select(item => item.GetRawText()).SequenceEqual(_full.GetProperty("page").GetProperty("items").EnumerateArray().Select(item => item.GetRawText())).ShouldBeTrue();
    [Fact] void should_refuse_a_stale_revision() => _stale!.Message.ShouldContain("StaleRevision");
    [Fact] void should_require_a_continuation_revision() => _unpinned!.Message.ShouldContain("expectedSourceRevision");
    [Fact] void should_answer_the_requested_version_on_every_page() => new[] { _full, _first, _second }.All(page => page.GetProperty("version").GetInt32() == 2).ShouldBeTrue();
    [Fact] void should_repeat_coverage_on_every_page() => new[] { _first, _second }.All(page => page.GetProperty("coverage").GetString() == "Complete" && page.GetProperty("hasWidening").GetBoolean()).ShouldBeTrue();
    [Fact] void should_repeat_the_revision_pin_on_every_page() => _second.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_keep_incomplete_coverage_on_every_page() => new[] { _incompleteFirst, _incompleteSecond }.All(page => page.GetProperty("coverage").GetString() == "Incomplete" && page.GetProperty("hasWidening").ValueKind == JsonValueKind.Null).ShouldBeTrue();
    [Fact] void should_keep_unknown_rows_on_every_incomplete_page() => new[] { _incompleteFirst, _incompleteSecond }.All(page => page.GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("change").GetString() == "Unknown").ShouldBeTrue();
    [Fact] void should_answer_the_requested_version_on_every_incomplete_page() => new[] { _incompleteFirst, _incompleteSecond }.All(page => page.GetProperty("version").GetInt32() == 2).ShouldBeTrue();
    [Fact] void should_keep_the_incomplete_revision_pin() => _incompleteSecond.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_incompleteFirst.GetProperty("sourceRevision").GetString());

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    void should_page_every_recovered_duplicate_candidate_as_unknown(bool baselineInvalid, bool sameFile)
    {
        const string firstBlock = "agents\n  tokens personal\n  query All\n    scope read\n    description \"Read\"\n";
        const string secondBlock = "agents\n  tokens personal\n  command Register\n    scope write\n    description \"Register\"\n";
        var valid = Create(Source + "\n" + firstBlock);
        var invalid = sameFile ? Create(Source + "\n" + firstBlock + "\n" + secondBlock) : DuplicateWorkspace(firstBlock, secondBlock);
        var before = baselineInvalid ? invalid : valid;
        var after = baselineInvalid ? valid : invalid;
        var first = ExposurePage(before, after, new { view = "agent-exposure", version = 2, limit = 1 });
        var repeated = ExposurePage(before, after, new { view = "agent-exposure", version = 2, limit = 1 });
        var revision = first.GetProperty("sourceRevision").GetString();
        var second = ExposurePage(before, after, new { view = "agent-exposure", version = 2, offset = 1, limit = 1, expectedSourceRevision = revision });
        first.GetRawText().ShouldEqual(repeated.GetRawText());
        first.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(2);
        foreach (var page in new[] { first, second })
        {
            page.GetProperty("version").GetInt32().ShouldEqual(2);
            page.GetProperty("coverage").GetString().ShouldEqual("Incomplete");
            page.GetProperty("hasWidening").ValueKind.ShouldEqual(JsonValueKind.Null);
            page.GetProperty("sourceRevision").GetString().ShouldEqual(revision);
            page.GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("change").GetString().ShouldEqual("Unknown");
        }
        new[] { first, second }.SelectMany(page => page.GetProperty("page").GetProperty("items").EnumerateArray()).Select(item => item.GetRawText()).Distinct().Count().ShouldEqual(2);
        var defaultView = JsonSerializer.SerializeToElement(McpSemanticDiff.Compare(before, after, JsonSerializer.SerializeToElement(new { limit = 200 })), McpJson.Options);
        defaultView.GetProperty("limits").EnumerateArray().Count(limit => limit.GetString() == "Agent exposure was not compared; use the agent-exposure view.").ShouldEqual(1);
    }

    static ScreenplayWorkspace DuplicateWorkspace(string first, string second) => ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source)),
         WorkspaceDocument.Create("first", PortablePlayPath.Parse("first.play"), Encoding.UTF8.GetBytes(first)),
         WorkspaceDocument.Create("second", PortablePlayPath.Parse("second.play"), Encoding.UTF8.GetBytes(second))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    static JsonElement ExposurePage(ScreenplayWorkspace before, ScreenplayWorkspace after, object arguments) =>
        JsonSerializer.SerializeToElement(McpSemanticDiff.Compare(before, after, JsonSerializer.SerializeToElement(arguments)), McpJson.Options);

    static JsonElement ReadIncomplete(object arguments) => JsonSerializer.SerializeToElement(McpSemanticDiff.Compare(
        Create(Source + "\n" + Exposure),
        Create(Source + "\n" + Exposure.Replace("tokens personal", "tokens oauth", StringComparison.Ordinal)),
        JsonSerializer.SerializeToElement(arguments)),
        McpJson.Options);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    void should_refuse_an_unsupported_version_on_every_page(int version)
    {
        var revision = _first.GetProperty("sourceRevision").GetString();
        Catch.Exception(() => Read(new { view = "agent-exposure", version }))!.Message.ShouldContain("'version' must equal 2");
        Catch.Exception(() => Read(new { view = "agent-exposure", version, offset = 1, expectedSourceRevision = revision }))!.Message.ShouldContain("'version' must equal 2");
    }

    [Fact]
    void should_require_a_version_on_every_page()
    {
        var revision = _first.GetProperty("sourceRevision").GetString();
        Catch.Exception(() => Read(new { view = "agent-exposure" }))!.Message.ShouldContain("'version' must equal 2");
        Catch.Exception(() => Read(new { view = "agent-exposure", offset = 1, expectedSourceRevision = revision }))!.Message.ShouldContain("'version' must equal 2");
    }
}
