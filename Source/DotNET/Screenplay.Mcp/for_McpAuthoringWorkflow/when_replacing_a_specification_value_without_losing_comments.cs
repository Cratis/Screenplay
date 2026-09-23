// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_replacing_a_specification_value_without_losing_comments : given.an_order_model_with_comments
{
    JsonElement _proposal;
    JsonElement _dropped;
    JsonElement _applied;

    void Because()
    {
        _proposal = ProposeChannels("PreserveTrivia");
        _dropped = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items");
        _applied = Apply(Opened, _proposal);
    }

    [Fact] void should_propose_the_change() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_canonicalize_the_document() => _proposal.GetProperty("canonicalizedSource").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);
    [Fact] void should_apply_the_change() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_rewrite_only_the_two_literals() => File.ReadAllBytes(OrderPath).ShouldEqual(Bytes(Order.Replace("channel = \"web\"", "channel = \"store\"", StringComparison.Ordinal)));
}
