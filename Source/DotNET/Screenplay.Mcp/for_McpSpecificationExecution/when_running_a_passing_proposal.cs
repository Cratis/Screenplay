// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_running_a_passing_proposal : given.a_proposal_model
{
    string _proposalId = string.Empty;
    JsonElement _first;
    JsonElement _next;
    JsonElement _selected;
    JsonElement _byId;
    JsonElement _scoped;
    JsonElement _wrongRevision;
    JsonElement _unpinned;

    void Because()
    {
        _proposalId = ProposeSource(ModelSource + "\n").GetProperty("proposalId").GetString()!;
        _first = Content("run-specifications", new { proposalId = _proposalId, limit = 1 });
        var revision = _first.GetProperty("sourceRevision").GetString();
        _next = Content("run-specifications", new { proposalId = _proposalId, offset = 1, limit = 1, expectedSourceRevision = revision });
        _selected = Content("run-specifications", new { proposalId = _proposalId, specification = "Projects.Registration.Register.Correct" });
        _byId = Content("run-specifications", new { proposalId = _proposalId, specification = _selected.GetProperty("page").GetProperty("items")[0].GetProperty("semanticId").GetString() });
        _scoped = Content("run-specifications", new { proposalId = _proposalId, scope = "Projects.Registration" });
        _wrongRevision = Content("run-specifications", new { proposalId = _proposalId, offset = 1, expectedSourceRevision = Content("run-specifications").GetProperty("sourceRevision").GetString() });
        _unpinned = Call("run-specifications", new { proposalId = _proposalId, offset = 1 });
    }

    [Fact] void should_echo_the_proposal_identity() => _first.GetProperty("proposalId").GetString().ShouldEqual(_proposalId);
    [Fact] void should_pass_the_whole_candidate() => _first.GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_count_both_scenarios_before_paging() => _first.GetProperty("passed").GetInt32().ShouldEqual(2);
    [Fact] void should_return_the_next_candidate_page() => _next.GetProperty("page").GetProperty("items")[0].GetProperty("address").GetString().ShouldEqual("Projects.Registration.Register.Wrong");
    [Fact] void should_pin_the_candidate_source_on_every_page() => _next.GetProperty("sourceRevision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_select_by_candidate_address() => _selected.GetProperty("selected").GetInt32().ShouldEqual(1);
    [Fact] void should_select_by_candidate_semantic_identity() => _byId.GetProperty("selected").GetInt32().ShouldEqual(1);
    [Fact] void should_select_a_candidate_scope() => _scoped.GetProperty("selected").GetInt32().ShouldEqual(2);
    [Fact] void should_refuse_the_current_source_revision_for_candidate_continuation() => _wrongRevision.GetProperty("failureKind").GetString().ShouldEqual("StaleSourceRevision");
    [Fact] void should_require_a_revision_on_candidate_continuation() => _unpinned.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
}
