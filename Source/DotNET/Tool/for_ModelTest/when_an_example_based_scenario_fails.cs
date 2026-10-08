// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_an_example_based_scenario_fails : given.a_model
{
    int _exit;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), response_scenarios.Example);
    void Because() => _exit = ModelTest.Run([Root], Output, Error);

    [Fact] void should_fail_the_scenario() => _exit.ShouldEqual(1);
    [Fact] void should_include_effective_fixture_provenance() => Output.ToString().ShouldContain("Effective fixtures:");
    [Fact] void should_name_the_example_and_effective_value() => Output.ToString().ShouldContain("Greeting: name = \"hello\" (example)");
}
