// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_paging_agent_exposure : given.a_semantic_comparison
{
    JsonElement _first;
    JsonElement _second;

    void Because()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents);
        _first = Read(new { includeAgentExposure = true, limit = 1 });
        _second = Read(new { includeAgentExposure = true, offset = 1, limit = 1, expectedSourceRevision = Proposal.Workspace.Revision.ToString() });
    }

    [Fact] void should_page_by_count() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_advance() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_finish() => _second.GetProperty("page").GetProperty("nextOffset").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_repeat_items() => _second.GetProperty("page").GetProperty("items")[0].GetRawText().ShouldNotEqual(_first.GetProperty("page").GetProperty("items")[0].GetRawText());
    [Fact] void should_repeat_deterministically() => Read(new { includeAgentExposure = true, limit = 1 }).GetRawText().ShouldEqual(_first.GetRawText());
    [Fact] void should_reassemble() => _first.GetProperty("page").GetProperty("items").EnumerateArray().Concat(_second.GetProperty("page").GetProperty("items").EnumerateArray()).Select(item => item.GetRawText()).ShouldEqual(Read(new { includeAgentExposure = true }).GetProperty("page").GetProperty("items").EnumerateArray().Select(item => item.GetRawText()));
    [Fact] void should_require_revision_on_continuation() => Catch.Exception(() => Read(new { includeAgentExposure = true, offset = 1 })).ShouldBeOfExactType<McpFailure>();
    [Fact] void should_refuse_stale_revision() => Catch.Exception(() => Read(new { includeAgentExposure = true, offset = 1, expectedSourceRevision = "stale" }))!.Message.ShouldContain("StaleRevision");

    [Fact]
    void should_bound_bytes_without_truncating_unicode()
    {
        var description = new string('ø', 20_000);
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents.Replace("List projects", description, StringComparison.Ordinal).Replace("Register projects", description, StringComparison.Ordinal));
        var first = Read(new { includeAgentExposure = true }).GetProperty("page");
        first.GetProperty("items").GetArrayLength().ShouldEqual(1);
        first.GetProperty("nextOffset").GetInt32().ShouldEqual(1);
        first.GetProperty("items")[0].GetProperty("after").GetProperty("description").GetString().ShouldEqual(description);
        Encoding.UTF8.GetByteCount(first.GetProperty("items").GetRawText()).ShouldBeLessThan(192 * 1024);
        Read(new { includeAgentExposure = true, offset = 1, expectedSourceRevision = Proposal.Workspace.Revision.ToString() }).GetProperty("page").GetProperty("items")[0].GetProperty("after").GetProperty("description").GetString().ShouldEqual(description);
    }

    [Fact]
    void should_refuse_a_single_oversized_item()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents.Replace("List projects", new string('ø', 40_000), StringComparison.Ordinal));
        Catch.Exception(() => Read(new { includeAgentExposure = true }))!.Message.ShouldContain("ResponseTooLarge");
    }
}
