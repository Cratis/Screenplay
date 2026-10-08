// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_querying_specification_routes;

public class and_steps_use_typed_examples : given.a_connection
{
    static readonly string[] _routeProperties = ["stream", "streamId", "no stream"];
    JsonElement _fixtures;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        eventsource Account
          identifier String
          stream Transactions
            streamId String
        module Banking
          feature History
            slice StateView Records
              event Recorded
                title String
                count Int
              example Prior : Recorded
                for "other"
                title = "original"
                count = 1
              specification Routed
                given Prior
                  stream Account.Transactions
                    streamId = "september"
                  title = "override"
                when append Prior count = 2
                  stream Account.Transactions
                    streamId = "october"
                then Prior count = 2
                  stream Account.Transactions
                    streamId = "october"
                then Prior
                  no stream
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _fixtures = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, null, null, null), McpJson.Options)
            .GetProperty("page").GetProperty("items");
    }

    [Fact] void should_report_the_effective_event_type() => Row("givenEvent", "title").GetProperty("target").GetString().ShouldEqual("Banking.History.Records.Recorded");
    [Fact] void should_keep_inherited_values() => Row("givenEvent", "count").GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_keep_override_origins() => Row("givenEvent", "title").GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_keep_the_replaced_value() => Row("givenEvent", "title").GetProperty("overriddenValue").GetString().ShouldEqual("original");
    [Fact] void should_keep_the_example_destination() => Row("givenEventDestination", "for").GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_report_the_authored_given_route() => Row("givenEventStream", "stream").GetProperty("value").GetString().ShouldEqual("Account.Transactions");
    [Fact] void should_report_the_authored_given_stream_id() => Row("givenEventStreamId", "streamId").GetProperty("value").GetString().ShouldEqual("september");
    [Fact] void should_report_the_appended_stream_id() => Row("whenAppendedEventStreamId", "streamId").GetProperty("value").GetString().ShouldEqual("october");
    [Fact] void should_report_the_expected_stream_id() => Row("thenEventStreamId", "streamId").GetProperty("value").GetString().ShouldEqual("october");
    [Fact] void should_report_the_unrouted_assertion() => Row("thenEventNoStream", "no stream").GetProperty("value").GetBoolean().ShouldBeTrue();
    [Fact] void should_mark_every_route_value_as_authored() => RouteRows().All(item => item.GetProperty("origin").GetString() == "authored").ShouldBeTrue();
    [Fact] void should_name_the_example_used_by_the_step() => RouteRows().All(item => item.GetProperty("example").GetString() == "Prior").ShouldBeTrue();

    IEnumerable<JsonElement> RouteRows() => _fixtures.EnumerateArray().Where(item => _routeProperties.Contains(item.GetProperty("property").GetString(), StringComparer.Ordinal));

    JsonElement Row(string role, string property) => _fixtures.EnumerateArray().First(item => item.GetProperty("role").GetString() == role && item.GetProperty("property").GetString() == property);
}
