// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_querying_specification_routes;

public class and_events_state_routes : given.a_connection
{
    JsonElement _fixtures;
    JsonElement _sources;
    JsonElement _streams;
    JsonElement _dependencies;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            eventsource Account
              identifier String
              stream Transactions
                streamId String
            module Banking
              feature History
                slice StateView Records
                  event Recorded
                    stream String
                  specification Routed
                    given Recorded
                      for "other"
                      stream Account.Transactions
                        streamId = "p-1:2026-10"
                      stream = "payload"
                    when append Recorded
                      for "other"
                      stream Account.Transactions
                        streamId = "p-1:2026-11"
                    then Recorded
                      stream Account.Transactions
                        streamId = "p-1:2026-11"
                    then Recorded
                      no stream
            """);
        Initialize();
    }

    void Because()
    {
        _fixtures = Content("find-fixtures", new { });
        _sources = Content("find-references", new { address = "Account", kind = "EventSource" });
        _streams = Content("find-references", new { address = "Account.Transactions", kind = "EventStream" });
        _dependencies = Content("dependencies", new { address = "Banking.History.Records.Routed", kind = "Specification", direction = "outgoing" });
    }

    [Fact] void should_keep_the_destination() => Row("givenEventDestination").GetProperty("value").GetString().ShouldEqual("other");
    [Fact] void should_keep_the_payload_stream_property() => Row("givenEvent").GetProperty("value").GetString().ShouldEqual("payload");
    [Fact] void should_expose_the_given_stream() => Row("givenEventStream").GetProperty("value").GetString().ShouldEqual("Account.Transactions");
    [Fact] void should_expose_the_given_stream_id() => Row("givenEventStreamId").GetProperty("value").GetString().ShouldEqual("p-1:2026-10");
    [Fact] void should_expose_the_appended_stream() => Row("whenAppendedEventStream").GetProperty("value").GetString().ShouldEqual("Account.Transactions");
    [Fact] void should_expose_the_appended_stream_id() => Row("whenAppendedEventStreamId").GetProperty("value").GetString().ShouldEqual("p-1:2026-11");
    [Fact] void should_expose_the_expected_stream() => Row("thenEventStream").GetProperty("value").GetString().ShouldEqual("Account.Transactions");
    [Fact] void should_expose_the_expected_stream_id() => Row("thenEventStreamId").GetProperty("value").GetString().ShouldEqual("p-1:2026-11");
    [Fact] void should_expose_the_unrouted_assertion() => Row("thenEventNoStream").GetProperty("value").GetBoolean().ShouldBeTrue();
    [Fact] void should_index_each_source_reference() => _sources.GetProperty("references").GetArrayLength().ShouldEqual(3);
    [Fact] void should_index_each_stream_reference() => _streams.GetProperty("references").GetArrayLength().ShouldEqual(3);
    [Fact] void should_locate_the_qualified_operand() => _streams.GetProperty("references")[0].GetProperty("location").GetProperty("column").GetInt32().ShouldEqual(18);
    [Fact] void should_label_the_stream_dependency() => _dependencies.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.ToString().Contains("specificationStream", StringComparison.Ordinal)).ShouldBeTrue();

    JsonElement Row(string role) => _fixtures.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("role").GetString() == role);
    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
}
