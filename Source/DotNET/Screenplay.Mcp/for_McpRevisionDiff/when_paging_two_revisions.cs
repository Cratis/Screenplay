// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_paging_two_revisions : given.two_snapshots
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _repeat;
    JsonElement _changedBefore;
    JsonElement _changedAfter;
    Exception? _missing;

    void Because()
    {
        _first = Compare(limit: 1).GetProperty("structuredContent");
        var revision = _first.GetProperty("sourceRevision").GetString();
        var offset = _first.GetProperty("page").GetProperty("nextOffset").GetInt32();
        _next = Compare(offset, 1, revision).GetProperty("structuredContent");
        _repeat = Compare(limit: 1).GetProperty("structuredContent");
        _missing = Catch.Exception(() => Compare(offset, 1));
        var before = BeforeJson;
        BeforeJson = AfterJson;
        _changedBefore = Compare(offset, 1, revision);
        BeforeJson = before;
        AfterJson = BeforeJson;
        _changedAfter = Compare(offset, 1, revision);
    }

    [Fact] void should_obey_the_count_limit() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_continue_at_the_next_offset() => _next.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
    [Fact] void should_preserve_the_pair_revision() => _next.GetProperty("sourceRevision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_repeat_deterministically() => _repeat.GetRawText().ShouldEqual(_first.GetRawText());
    [Fact] void should_require_a_revision_on_continuation() => _missing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_reject_a_changed_baseline() => _changedBefore.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
    [Fact] void should_reject_a_changed_candidate() => _changedAfter.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
}
