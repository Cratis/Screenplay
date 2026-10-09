// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_querying_persona_callers : given.a_connection
{
    JsonElement _details;
    JsonElement _values;
    McpSyntaxIndex _index = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        policy Member
          require role "A" or role "B"
        persona Person
          policy Member
        module M
          feature F
            slice StateChange S
              command C
                authorize Member
                produces E
              event E
              specification X
                given caller as Person
                when C
                then E
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _index = snapshot.Index;
        _details = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(snapshot, JsonSerializer.SerializeToElement(new { address = "Person", kind = "Persona", view = "caller" })), McpJson.Options).GetProperty("details");
        _values = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, "M.F.S.X", "givenCaller", null, null), McpJson.Options).GetProperty("page").GetProperty("items");
    }

    [Fact] void should_show_an_authenticated_witness() => _details.GetProperty("caller").GetProperty("authenticated").GetBoolean().ShouldBeTrue();
    [Fact] void should_show_only_the_selected_role() => _details.GetProperty("caller").GetProperty("roles").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly("A");
    [Fact] void should_name_the_contributing_policy() => _details.GetProperty("contributions")[0].GetProperty("policy").GetString().ShouldEqual("Member");
    [Fact] void should_not_report_a_refusal() => _details.GetProperty("refusal").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_persona_fixture_origins() => _values[0].GetProperty("origin").GetString().ShouldEqual("persona");
    [Fact] void should_report_the_persona_name() => _values[0].GetProperty("persona").GetString().ShouldEqual("Person");
    [Fact] void should_report_the_policy_name() => _values[0].GetProperty("policy").GetString().ShouldEqual("Member");
    [Fact] void should_index_the_persona_reference() => _index.Incoming(_index.Find("Person", "Persona").Single()).Single().Reference.Role.ShouldEqual("givenCallerPersona");
}
