// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_an_example_based_scenario_fails : given.a_model
{
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), given.response_scenarios.Example);
    void Because() => _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_fail_the_scenario() => _report.GetProperty("failed").GetInt32().ShouldEqual(1);
    [Fact] void should_include_effective_fixture_provenance() => _report.GetProperty("page").GetProperty("items")[0].GetProperty("failures")[0].GetString()!.ShouldContain("Effective fixtures:");
    [Fact] void should_name_the_example_and_effective_value() => _report.GetProperty("page").GetProperty("items")[0].GetProperty("failures")[0].GetString()!.ShouldContain("Greeting: name = \"hello\" (example)");
}
