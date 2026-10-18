// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context.given;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_one_projection_is_unadmitted : given.a_model
{
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), a_model.Source);
    void Because() => _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_unsupported() => _report.GetProperty("outcome").GetString().ShouldEqual("unsupported");
    [Fact] void should_discover_all_scenarios() => _report.GetProperty("discovered").GetInt32().ShouldEqual(10);
    [Fact] void should_execute_the_unrelated_scenario() => _report.GetProperty("executed").GetInt32().ShouldEqual(1);
    [Fact] void should_pass_the_unrelated_scenario() => _report.GetProperty("passed").GetInt32().ShouldEqual(1);
    [Fact] void should_refuse_only_the_dependent_scenarios() => _report.GetProperty("unsupported").GetInt32().ShouldEqual(9);
    [Fact] void should_not_report_failures() => _report.GetProperty("failed").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_plan_issues_visible() => _report.GetProperty("planIssues").GetArrayLength().ShouldEqual(1);
}
