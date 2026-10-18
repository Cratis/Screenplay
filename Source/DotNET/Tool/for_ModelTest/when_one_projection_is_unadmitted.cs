// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context.given;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_one_projection_is_unadmitted : given.a_model
{
    int _exit;
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), a_model.Source);

    void Because()
    {
        _exit = ModelTest.Run([Root, "--format", "json"], Output, Error);
        using var document = JsonDocument.Parse(Output.ToString());
        _report = document.RootElement.Clone();
    }

    [Fact] void should_exit_three() => _exit.ShouldEqual(3);
    [Fact] void should_execute_the_unrelated_scenario() => _report.GetProperty("executed").GetInt32().ShouldEqual(1);
    [Fact] void should_pass_the_unrelated_scenario() => _report.GetProperty("passed").GetInt32().ShouldEqual(1);
    [Fact] void should_refuse_only_the_dependent_scenarios() => _report.GetProperty("unsupported").GetInt32().ShouldEqual(9);
    [Fact] void should_not_report_failures() => _report.GetProperty("failed").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_plan_issues_visible() => _report.GetProperty("planIssues").GetArrayLength().ShouldEqual(1);
    [Fact] void should_keep_errors_separate_from_json() => Error.ToString().ShouldBeEmpty();
}
