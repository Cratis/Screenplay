// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_querying_example_fixtures : given.a_connection
{
    JsonElement _values;
    JsonElement _details;
    JsonElement _search;
    McpSyntaxIndex _index = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        concept ItemId : Uuid
        module Records
          feature Entries
            slice StateChange Record
              command RecordItem
                id ItemId generated identifier
                receipt ItemId generated
                title String
                count Int
                produces ItemRecorded
                  for id
                  title = title
                  count = count
              event ItemRecorded
                title String
                count Int
              readmodel Item
                title String
                count Int
              example Recorded : ItemRecorded
                title = "original"
                count = 1
                for "00000000-0000-0000-0000-000000000001"
              example Input : RecordItem
                title = "original"
                count = 1
                generated receipt = "00000000-0000-0000-0000-000000000001"
              example View : Item
                title = "original"
                count = 1
              specification Recording
                given Recorded
                given readmodel View
                when Input count = 2
                then Recorded count = 2
                then readmodel View count = 2
              specification Appending
                when append Recorded count = 2
                then Recorded count = 2
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _index = snapshot.Index;
        _details = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(snapshot, JsonSerializer.SerializeToElement(new { address = "Records.Entries.Record.Input", kind = "Example" })), McpJson.Options);
        _search = JsonSerializer.SerializeToElement(McpModelQueries.Search(snapshot, JsonSerializer.SerializeToElement(new { kind = "Example", name = "Inp", match = "prefix" })), McpJson.Options);
        _values = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, null, null, null), McpJson.Options).GetProperty("page").GetProperty("items");
    }

    [Fact] void should_show_the_effective_command_type() => Find("whenCommand", "count").GetProperty("target").GetString().ShouldEqual("Records.Entries.Record.RecordItem");
    [Fact] void should_show_the_override_value() => Find("whenCommand", "count").GetProperty("value").GetInt32().ShouldEqual(2);
    [Fact] void should_show_the_override_origin() => Find("whenCommand", "count").GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_show_the_replaced_value() => Find("whenCommand", "count").GetProperty("overriddenValue").GetInt32().ShouldEqual(1);
    [Fact] void should_show_inherited_values() => Find("whenCommand", "title").GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_name_the_example() => Find("whenCommand", "title").GetProperty("example").GetString().ShouldEqual("Input");
    [Fact] void should_show_generated_origins() => Find("generatedValues", "receipt").GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_show_destination_origins() => Find("givenEventDestination", "for").GetProperty("origin").GetString().ShouldEqual("example");
    [Fact] void should_show_appended_event_origins() => Find("whenAppendedEvent", "count").GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_show_read_model_origins() => Find("thenReadModel", "count").GetProperty("origin").GetString().ShouldEqual("override");
    [Fact] void should_index_examples() => _index.Find("Records.Entries.Record.Input", "Example").Length.ShouldEqual(1);
    [Fact] void should_expose_the_underlying_type_in_details() => _details.GetProperty("details").GetProperty("exampleType").GetString().ShouldEqual("RecordItem");
    [Fact] void should_include_examples_in_scoped_search() => _search.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_link_uses_to_examples() => _index.Incoming(_index.Find("Records.Entries.Record.Input", "Example").Single()).Single().Reference.Role.ShouldEqual("whenCommand");
    [Fact] void should_link_examples_to_underlying_types() => _index.Outgoing("Records.Entries.Record.Input").Single().Role.ShouldEqual("exampleType");
    [Fact] void should_resolve_effective_field_types() => Find("whenCommand", "count").GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("Int");

    JsonElement Find(string role, string property) => _values.EnumerateArray().First(item => item.GetProperty("role").GetString() == role && item.GetProperty("property").GetString() == property);
}
