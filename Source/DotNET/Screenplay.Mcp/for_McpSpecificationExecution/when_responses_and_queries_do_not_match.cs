// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_responses_and_queries_do_not_match : given.a_model
{
    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), given.response_scenarios.Source);

    [Theory]
    [InlineData("Scalar", "response")]
    [InlineData("Record", "response")]
    [InlineData("Query", "query results")]
    void should_report_expected_and_actual_values(string slice, string label)
    {
        var report = Call("run-specifications", new { specification = $"M.F.{slice}.Wrong" }).GetProperty("result").GetProperty("structuredContent");
        report.GetProperty("outcome").GetString().ShouldEqual("failed");
        var failures = report.GetProperty("page").GetProperty("items")[0].GetProperty("failures").EnumerateArray().Select(value => value.GetString()!).ToArray();
        failures.Single(value => value.StartsWith($"Expected {label}:", StringComparison.Ordinal)).ShouldContain("different");
        failures.Single(value => value.StartsWith($"Actual {label}:", StringComparison.Ordinal)).ShouldContain("hello");
    }
}
