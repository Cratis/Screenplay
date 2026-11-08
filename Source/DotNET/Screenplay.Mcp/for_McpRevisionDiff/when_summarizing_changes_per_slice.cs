// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_summarizing_changes_per_slice : given.two_snapshots
{
    JsonElement _slices;
    JsonElement _proposal;

    void Because()
    {
        _slices = Compare().GetProperty("structuredContent").GetProperty("slices");
        _proposal = Read().GetProperty("slices");
    }

    JsonElement Slice => _slices.GetProperty("slices").EnumerateArray().Single();

    [Fact] void should_report_the_slice_that_owns_the_event() => Slice.GetProperty("afterAddress").GetString()!.EndsWith(".Register", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_it_as_changed() => Slice.GetProperty("change").GetString().ShouldEqual("changed");
    [Fact] void should_list_every_change_kind() => Slice.GetProperty("changes").EnumerateArray().Select(kind => kind.GetString()).ShouldContainOnly("changed");
    [Fact] void should_list_the_changed_event() => Slice.GetProperty("declarations").EnumerateArray().ShouldContain(declaration => declaration.GetProperty("kind").GetString() == "Event");
    [Fact] void should_list_dependent_slices() => Slice.GetProperty("dependentSlices").ValueKind.ShouldEqual(JsonValueKind.Array);
    [Fact] void should_report_model_wide_changes() => _slices.GetProperty("modelWide").ValueKind.ShouldEqual(JsonValueKind.Array);
    [Fact] void should_report_affected_slices() => _slices.GetProperty("affectedSlices").ValueKind.ShouldEqual(JsonValueKind.Array);
    [Fact] void should_match_the_proposal_view() => _slices.GetRawText().ShouldEqual(_proposal.GetRawText());
}
