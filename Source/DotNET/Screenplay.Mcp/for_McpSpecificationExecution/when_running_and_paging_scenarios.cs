// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_running_and_paging_scenarios : given.a_model
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _selected;
    JsonElement _byId;
    JsonElement _scoped;
    JsonElement _unknown;
    JsonElement _unpinned;
    JsonElement _stale;

    void Because()
    {
        _first = Call("run-specifications", new { limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var revision = _first.GetProperty("sourceRevision").GetString();
        _next = Call("run-specifications", new { offset = 1, limit = 1, expectedSourceRevision = revision }).GetProperty("result").GetProperty("structuredContent");
        _selected = Call("run-specifications", new { specification = "Projects.Registration.Register.Correct" }).GetProperty("result").GetProperty("structuredContent");
        _byId = Call("run-specifications", new { specification = _selected.GetProperty("page").GetProperty("items")[0].GetProperty("semanticId").GetString() }).GetProperty("result").GetProperty("structuredContent");
        _scoped = Call("run-specifications", new { scope = "Projects.Registration" }).GetProperty("result").GetProperty("structuredContent");
        _unknown = Call("run-specifications", new { specification = "Missing" });
        _unpinned = Call("run-specifications", new { offset = 1 });
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n");
        _stale = Call("run-specifications", new { expectedSourceRevision = revision });
    }

    [Fact] void should_count_every_discovered_scenario() => _first.GetProperty("discovered").GetInt32().ShouldEqual(2);
    [Fact] void should_execute_both_scenarios_before_paging() => _first.GetProperty("executed").GetInt32().ShouldEqual(2);
    [Fact] void should_report_the_whole_selection_as_failed() => _first.GetProperty("outcome").GetString().ShouldEqual("failed");
    [Fact] void should_pass_the_first_scenario() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_fail_the_wrong_expectation() => _next.GetProperty("page").GetProperty("items")[0].GetProperty("outcome").GetString().ShouldEqual("failed");
    [Fact] void should_explain_the_expected_and_actual_difference() => _next.GetProperty("page").GetProperty("items")[0].GetProperty("failures").ToString().ShouldContain("Other");
    [Fact] void should_filter_by_exact_address() => _selected.GetProperty("selected").GetInt32().ShouldEqual(1);
    [Fact] void should_filter_by_semantic_identity() => _byId.GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_select_a_scope() => _scoped.GetProperty("selected").GetInt32().ShouldEqual(2);
    [Fact] void should_refuse_unknown_selections() => _unknown.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_require_a_revision_on_continuation() => _unpinned.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_refuse_stale_source() => _stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_write_workspace_state() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
}
