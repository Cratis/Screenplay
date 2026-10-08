// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_scoping_shared_concept_evidence : for_McpConnection.given.a_connection
{
    JsonElement[] _scoped = [];
    JsonElement[] _unscoped = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            concept Name : String
              validate
                not empty message "Name is required"
                require value != "Reserved"
                  message "Name is reserved"
            module Projects
              feature Maintenance
                slice StateChange Register
                  command Register
                    name Name
                slice StateChange Rename
                  command Rename
                    name Name
                  specification Empty
                    when Rename
                      name = ""
                    then error "Name is required"
                  specification Reserved
                    when Rename
                      name = "Reserved"
                    then error "Name is reserved"
            """);
        Initialize();
    }

    void Because()
    {
        _scoped = ConceptItems(Call("find-specification-obligations", new { scope = "Projects.Maintenance.Register" }));
        _unscoped = ConceptItems(Call("find-specification-obligations"));
    }

    [Fact] void should_include_both_concept_rejection_rules() => _scoped.Length.ShouldEqual(2);
    [Fact] void should_find_evidence_from_other_commands_accepting_the_concept() => _scoped.All(item => item.GetProperty("status").GetString() == "met").ShouldBeTrue();
    [Fact] void should_keep_scoped_evidence_identical_to_unscoped_evidence() => _scoped.Select(item => item.ToString()).ShouldContainOnly(_unscoped.Select(item => item.ToString()));

    static JsonElement[] ConceptItems(JsonElement response) => [.. response.GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("declaration").GetProperty("kind").GetString() == "Concept")];
}
