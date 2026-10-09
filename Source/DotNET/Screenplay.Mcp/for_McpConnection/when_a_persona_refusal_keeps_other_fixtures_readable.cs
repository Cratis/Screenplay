// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_a_persona_refusal_keeps_other_fixtures_readable : given.a_connection
{
    McpSnapshot _snapshot;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        policy Restricted
          require not role "A"
        persona Person
          policy Restricted
        module M
          feature F
            slice StateChange S
              command C
                value String
                produces E
                  value = value
              event E
                value String
              specification Refused
                given caller as Person
                when C value = "refused"
                then E value = "refused"
              specification Available
                when C value = "available"
                then E value = "available"
        """);

    void Because() => _snapshot = new(Root.Read());

    [Fact]
    void should_page_the_other_specifications_fixtures()
    {
        var result = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(_snapshot, "M.F.S.Available", null, null, null), McpJson.Options);
        result.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    }

    [Fact]
    void should_keep_obligations_readable() => McpSpecificationObligations.Read(_snapshot, JsonSerializer.SerializeToElement(new { })).ShouldNotBeNull();
}
