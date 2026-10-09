// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_additional_descriptions : given.a_connection
{
    McpSnapshot _snapshot;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        concept Value : String
          description "Concept intent"
        policy Access
          description "Policy intent"
          require authenticated
        module M
          form Input for Record
            description "Form intent"
          feature F
            slice StateChange S
              command Record
              event E
              constraint C
                description "Constraint intent"
                unique event E
              projection P
                description "Projection intent"
                from E
              screen Home
                description "Screen intent"
        """);

    void Because() => _snapshot = new(Root.Read());

    [Theory]
    [InlineData("Value", "Concept", "Concept")]
    [InlineData("Access", "Policy", "Policy")]
    [InlineData("M.Input", "Form", "Form")]
    [InlineData("M.F.S.C", "Constraint", "Constraint")]
    [InlineData("M.F.S.P", "Projection", "Projection")]
    [InlineData("M.F.S.Home", "Screen", "Screen")]
    void should_surface_each_description(string address, string kind, string label) => JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, JsonSerializer.SerializeToElement(new { address, kind }))).GetProperty("details").GetProperty("description").GetString().ShouldEqual($"{label} intent");
}
