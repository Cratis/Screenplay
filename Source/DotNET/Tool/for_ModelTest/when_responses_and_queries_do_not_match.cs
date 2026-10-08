// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_responses_and_queries_do_not_match : given.a_model
{
    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), response_scenarios.Source);

    [Theory]
    [InlineData("Scalar", "response")]
    [InlineData("Record", "response")]
    [InlineData("Query", "query results")]
    void should_report_expected_and_actual_values(string slice, string label)
    {
        ModelTest.Run([Root, "--filter", $"M.F.{slice}.Wrong", "--format", "json"], Output, Error).ShouldEqual(1);
        using var document = JsonDocument.Parse(Output.ToString());
        var failures = document.RootElement.GetProperty("results")[0].GetProperty("failures").EnumerateArray().Select(value => value.GetString()).ToArray();
        failures.Single(value => value.StartsWith($"Expected {label}:", StringComparison.Ordinal)).ShouldContain("different");
        failures.Single(value => value.StartsWith($"Actual {label}:", StringComparison.Ordinal)).ShouldContain("hello");
    }
}
