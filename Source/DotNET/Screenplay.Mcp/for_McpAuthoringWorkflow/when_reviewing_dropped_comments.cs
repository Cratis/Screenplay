// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reviewing_dropped_comments : given.an_order_model_with_comments
{
    JsonElement _proposal;
    JsonElement[] _dropped = [];
    bool _unchangedBeforeApply;

    void Because()
    {
        _proposal = ProposeChannels("CanonicalizeTouchedDocuments");
        _dropped = [.. Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items").EnumerateArray()];
        _unchangedBeforeApply = File.ReadAllBytes(OrderPath).SequenceEqual(Bytes(Order));
    }

    [Fact] void should_disclose_canonicalization() => _proposal.GetProperty("canonicalizedSource").GetBoolean().ShouldBeTrue();
    [Fact] void should_count_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.ShouldBeEmpty();
    [Fact] void should_leave_disk_unchanged_before_apply() => _unchangedBeforeApply.ShouldBeTrue();
}
