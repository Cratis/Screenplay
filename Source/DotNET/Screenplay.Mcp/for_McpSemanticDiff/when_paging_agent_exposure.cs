// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_paging_agent_exposure : given.an_agent_exposure_comparison
{
    JsonElement _first;
    JsonElement _second;

    void Because()
    {
        CompareSnapshots(Source, Source + when_comparing_agent_exposure.Agents);
        _first = ReadExposure(new { limit = 1 });
        _second = ReadExposure(new { offset = 1, limit = 1, expectedSourceRevision = Proposal.Workspace.Revision.ToString() });
    }

    [Fact] void should_page_by_count() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_advance() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_finish() => _second.GetProperty("page").GetProperty("nextOffset").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_repeat_items() => _second.GetProperty("page").GetProperty("items")[0].GetRawText().ShouldNotEqual(_first.GetProperty("page").GetProperty("items")[0].GetRawText());
    [Fact] void should_repeat_deterministically() => ReadExposure(new { limit = 1 }).GetRawText().ShouldEqual(_first.GetRawText());
    [Fact] void should_reassemble() => _first.GetProperty("page").GetProperty("items").EnumerateArray().Concat(_second.GetProperty("page").GetProperty("items").EnumerateArray()).Select(item => item.GetRawText()).ShouldEqual(ReadExposure().GetProperty("page").GetProperty("items").EnumerateArray().Select(item => item.GetRawText()));
    [Fact] void should_require_revision_on_continuation() => Catch.Exception(() => ReadExposure(new { offset = 1 })).ShouldBeOfExactType<McpFailure>();
    [Fact] void should_refuse_stale_revision() => Catch.Exception(() => ReadExposure(new { offset = 1, expectedSourceRevision = "stale" }))!.Message.ShouldContain("StaleRevision");
}
