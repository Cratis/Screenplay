// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_failures_and_unsupported_scenarios_coexist : given.a_model
{
    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), response_scenarios.Mixed);

    [Fact] void should_exit_one_if_any_scenario_failed() => ModelTest.Run([Root], Output, Error).ShouldEqual(1);
    [Fact] void should_exit_three_if_only_unsupported_scenarios_are_selected() => ModelTest.Run([Root, "--filter", "M.F.Responses.MissingFixture"], Output, Error).ShouldEqual(3);
}
