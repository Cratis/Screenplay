// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_running_an_unavailable_proposal : given.a_proposal_model
{
    JsonElement _unknown;
    JsonElement _discarded;
    JsonElement _applied;

    void Because()
    {
        _unknown = Content("run-specifications", new { proposalId = "missing" });
        var proposalId = ProposeSource(ModelSource + "\n").GetProperty("proposalId").GetString()!;
        Content("discard-proposal", new { proposalId });
        _discarded = Content("run-specifications", new { proposalId });
        proposalId = ProposeSource(ModelSource + "\n").GetProperty("proposalId").GetString()!;
        ApplyProposal(proposalId).GetProperty("success").GetBoolean().ShouldBeTrue();
        _applied = Content("run-specifications", new { proposalId });
    }

    [Fact] void should_refuse_an_unknown_proposal() => _unknown.GetProperty("failureKind").GetString().ShouldEqual("UnknownProposal");
    [Fact] void should_refuse_a_discarded_proposal() => _discarded.GetProperty("failureKind").GetString().ShouldEqual("UnknownProposal");
    [Fact] void should_refuse_an_applied_proposal() => _applied.GetProperty("failureKind").GetString().ShouldEqual("UnknownProposal");
}
