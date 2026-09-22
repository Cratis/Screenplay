// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_querying_slices_without_assertions : given.a_connection
{
    JsonElement _first;
    JsonElement _second;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        module Billing
          feature Accounts
            slice StateChange Empty
              command Open
            slice StateChange SetupOnly
              event Opened
              specification Preparation
                given Opened
            slice StateChange Asserted
              command Close
              specification Rejected
                when Close
                then error
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _first = JsonSerializer.SerializeToElement(McpFixtureQueries.AssertionGaps(snapshot, 0, 1), McpJson.Options);
        _second = JsonSerializer.SerializeToElement(McpFixtureQueries.AssertionGaps(snapshot, 1, 1), McpJson.Options);
    }

    [Fact] void should_count_every_slice() => _first.GetProperty("page").GetProperty("total").GetInt32().ShouldEqual(3);
    [Fact] void should_count_only_slices_without_assertions() => _first.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(2);
    [Fact] void should_report_more_results() => _first.GetProperty("page").GetProperty("truncated").GetBoolean().ShouldBeTrue();
    [Fact] void should_include_a_slice_without_specifications() => _first.GetProperty("page").GetProperty("items")[0].GetProperty("slice").GetProperty("name").GetString().ShouldEqual("Empty");
    [Fact] void should_include_a_specification_that_only_arranges_state() => _second.GetProperty("page").GetProperty("items")[0].GetProperty("slice").GetProperty("name").GetString().ShouldEqual("SetupOnly");
    [Fact] void should_keep_the_number_of_authored_specifications() => _second.GetProperty("page").GetProperty("items")[0].GetProperty("specifications").GetInt32().ShouldEqual(1);
}
