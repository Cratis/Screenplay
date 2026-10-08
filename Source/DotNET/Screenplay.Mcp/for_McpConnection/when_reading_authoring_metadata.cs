// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_authoring_metadata : given.a_connection
{
    McpSnapshot _snapshot = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "model.play"), """
        module M
          description "Module summary"
          documentation
            ```markdown
            Module reasoning
            ```
          feature F
            description "Feature summary"
            documentation
              ```markdown
              Feature reasoning
              ```
            slice StateChange S
              description "Slice summary"
              documentation
                ```markdown
                Slice reasoning
                ```
              command C
                description "Command summary"
                documentation
                  ```markdown
                  Command reasoning
                  ```
              event E
                description "Event summary"
                documentation
                  ```markdown
                  Event reasoning
                  ```
              readmodel V
                description "View summary"
                documentation
                  ```markdown
                  View reasoning
                  ```
              reaction R
                description "Reaction summary"
                documentation
                  ```markdown
                  Reaction reasoning
                  ```
                when E
              specification Case
                description "Case summary"
        """);

    void Because() => _snapshot = new(Root.Read());

    [Theory]
    [InlineData("M", "Module", "Module")]
    [InlineData("M.F", "Feature", "Feature")]
    [InlineData("M.F.S", "Slice", "Slice")]
    [InlineData("M.F.S.C", "Command", "Command")]
    [InlineData("M.F.S.E", "Event", "Event")]
    [InlineData("M.F.S.V", "ReadModel", "View")]
    [InlineData("M.F.S.R", "Reaction", "Reaction")]
    void should_return_both_authoring_fields(string address, string kind, string label)
    {
        var details = Details(address, kind);
        details.GetProperty("description").GetString().ShouldEqual($"{label} summary");
        details.GetProperty("documentation").GetString().ShouldEqual($"{label} reasoning");
    }

    [Fact] void should_return_the_specification_description() => Details("M.F.S.Case", "Specification").GetProperty("description").GetString().ShouldEqual("Case summary");
    [Fact] void should_not_invent_specification_documentation() => Details("M.F.S.Case", "Specification").GetProperty("documentation").ValueKind.ShouldEqual(JsonValueKind.Null);

    JsonElement Details(string address, string kind) => JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, JsonSerializer.SerializeToElement(new { address, kind, view = "summary" }))).GetProperty("details");
}
