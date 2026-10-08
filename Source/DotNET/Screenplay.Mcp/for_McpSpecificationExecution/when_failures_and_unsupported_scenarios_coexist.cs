// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_failures_and_unsupported_scenarios_coexist : given.a_model
{
    JsonElement _report;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), given.response_scenarios.Mixed);
    void Because() => _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_give_failure_precedence() => _report.GetProperty("outcome").GetString().ShouldEqual("failed");
    [Fact] void should_count_the_failure() => _report.GetProperty("failed").GetInt32().ShouldEqual(1);
    [Fact] void should_keep_the_unsupported_result() => _report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
    [Fact] void should_only_count_the_executed_failure() => _report.GetProperty("executed").GetInt32().ShouldEqual(1);
}
