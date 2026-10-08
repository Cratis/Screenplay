// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_paging_a_comparison : given.a_semantic_comparison
{
    JsonElement _second;
    JsonElement _repeated;
    string[] _full = [];
    readonly List<string> _joined = [];
    bool _advances;
    Exception? _stale;
    Exception? _unpinned;

    void Because()
    {
        Propose(Source.Replace("event Registered\n        name String", "event Registered\n        name String\n        extra String", StringComparison.Ordinal));
        Diff = Read(new { limit = 1 });
        _repeated = Read(new { limit = 1 });
        var revision = Diff.GetProperty("sourceRevision").GetString();
        _second = Read(new { offset = 1, limit = 1, expectedSourceRevision = revision });
        _full = [.. Read(new { limit = 200 }).GetProperty("page").GetProperty("items").EnumerateArray().Select(item => item.GetRawText())];
        _advances = true;
        var offset = 0;
        while (true)
        {
            var page = Read(new { offset, limit = 1, expectedSourceRevision = revision }).GetProperty("page");
            _joined.AddRange(page.GetProperty("items").EnumerateArray().Select(item => item.GetRawText()));
            var next = page.GetProperty("nextOffset");
            if (next.ValueKind == JsonValueKind.Null) break;
            if (next.GetInt32() <= offset)
            {
                _advances = false;
                break;
            }
            offset = next.GetInt32();
        }
        _stale = Catch.Exception(() => Read(new { offset = 1, limit = 1, expectedSourceRevision = "stale" }));
        _unpinned = Catch.Exception(() => Read(new { offset = 1, limit = 1 }));
    }

    [Fact] void should_produce_identical_bytes_for_identical_inputs() => _repeated.GetRawText().ShouldEqual(Diff.GetRawText());
    [Fact] void should_return_the_first_item_of_the_full_list_on_the_first_page() => Diff.GetProperty("page").GetProperty("items")[0].GetRawText().ShouldEqual(_full[0]);
    [Fact] void should_return_the_second_item_of_the_full_list_on_the_second_page() => _second.GetProperty("page").GetProperty("items")[0].GetRawText().ShouldEqual(_full[1]);
    [Fact] void should_reassemble_the_complete_ordered_list_without_skips_or_duplicates() => _joined.ToArray().ShouldEqual(_full);
    [Fact] void should_advance_every_continuation_offset() => _advances.ShouldBeTrue();
    [Fact] void should_advance_the_first_offset_to_the_second_item() => Diff.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_bind_pages_to_the_proposal_revision() => _second.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(Proposal.Workspace.Revision.ToString());
    [Fact] void should_refuse_stale_continuation() => _stale!.Message.ShouldContain("StaleRevision");
    [Fact] void should_require_a_revision_on_continuation() => _unpinned!.Message.ShouldContain("expectedSourceRevision");
}
