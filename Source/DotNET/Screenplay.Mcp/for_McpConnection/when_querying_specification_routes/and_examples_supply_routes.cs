// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_querying_specification_routes;

public class and_examples_supply_routes : given.a_connection
{
    JsonElement _fixtures;
    JsonElement _locators;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        eventsource Account
          identifier String
          stream Ledger
            streamId String
        module M
          feature F
            slice Automation S
              event Recorded
                amount Int
              reaction Observer
                when Recorded
                  produces Observed
              event Observed
              example September : Recorded
                for "account"
                stream Account.Ledger
                  streamId = "september"
                amount = 1
              specification Recovering
                given September
                given September
                  stream Account.Ledger
                    streamId = "october"
                when redelivered Recorded to Observer
                  stream Account.Ledger
                    streamId = "september"
                  amount = 1
                then September
                  no stream
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _fixtures = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, null, null, null), McpJson.Options).GetProperty("page").GetProperty("items");
        _locators = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, "whenRedeliveredEvent", null, null), McpJson.Options).GetProperty("page").GetProperty("items");
    }

    [Fact] void should_keep_the_inherited_route_origin() => Rows("givenEventStream").First().GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_keep_the_inherited_id_origin() => Rows("givenEventStreamId").First().GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_name_the_example() => Rows("givenEventStream").First().GetProperty("example").GetString().ShouldEqual("September");
    [Fact] void should_keep_the_replacement_origin() => Rows("givenEventStream").Last().GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_keep_the_whole_replaced_route() => Rows("givenEventStream").Last().GetProperty("overriddenValue").GetString().ShouldEqual("Account.Ledger streamId = \"september\"");
    [Fact] void should_replace_with_no_stream() => Rows("thenEventNoStream").Single().GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_report_the_replaced_route_only_once() => Rows("givenEventStreamId").Last().GetProperty("overriddenValue").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_filter_locator_payloads_by_the_new_role() => _locators.GetArrayLength().ShouldEqual(1);
    [Fact] void should_report_locator_routes() => Rows("whenRedeliveredEventStream").Single().GetProperty("value").GetString().ShouldEqual("Account.Ledger");

    IEnumerable<JsonElement> Rows(string role) => _fixtures.EnumerateArray().Where(item => item.GetProperty("role").GetString() == role);
}
