// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_indexing_inline_events : given.a_connection
{
    McpSnapshot _snapshot = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "rename.play"), """
        module Projects
          feature Naming
            slice StateChange Rename
              command Rename
                projectId Uuid identifier
                name String
                produces event Renamed
                  description "A new name"
                  documentation
                    ```markdown
                    Keeps the event source.
                    ```
                  name String = name
        """);

    void Because() => _snapshot = new(Root.Read());

    [Fact] void should_index_the_inline_event_once() => _snapshot.Index.Declarations.Count(declaration => declaration.Name == "Renamed" && declaration.Kind == "Event").ShouldEqual(1);
    [Fact] void should_own_its_address_in_the_slice() => _snapshot.Index.Declarations.Single(declaration => declaration.Name == "Renamed").Address.ShouldEqual("Projects.Naming.Rename.Renamed");
    [Fact] void should_expose_the_description() => _snapshot.Index.Declarations.Single(declaration => declaration.Name == "Renamed").Description.ShouldEqual("A new name");
    [Fact] void should_link_both_command_and_slice_declarations() => _snapshot.Index.References.Where(reference => reference.Name == "Renamed" && reference.Role == "declares").Select(reference => reference.Owner!.Kind).ShouldContainOnly("Command", "Slice");
    [Fact] void should_resolve_the_production() => _snapshot.Index.Resolve(_snapshot.Index.References.Single(reference => reference.Name == "Renamed" && reference.Role == "produces")).Single().Name.ShouldEqual("Renamed");
    [Fact]
    void should_return_typed_properties_and_documentation()
    {
        using var arguments = JsonDocument.Parse("""{"address":"Projects.Naming.Rename.Renamed","kind":"Event","view":"summary"}""");
        var details = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, arguments.RootElement));
        details.GetProperty("details").GetProperty("propertyCount").GetInt32().ShouldEqual(1);
        details.GetProperty("details").GetProperty("documentation").GetString().ShouldEqual("Keeps the event source.");
    }
}
