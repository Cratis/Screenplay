// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_reading_duplicate_event_declarations : given.a_connection
{
    McpSnapshot _snapshot = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "duplicate.play"), """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """);

    void Because() => _snapshot = new(Root.Read());

    [Fact] void should_keep_both_physical_event_declarations() => _snapshot.Index.Declarations.Count(declaration => declaration.Kind == "Event" && declaration.Name == "ProjectRegistered").ShouldEqual(2);
    [Fact] void should_report_failed_compilation() => _snapshot.Compilation.Success.ShouldBeFalse();
    [Fact] void should_preserve_duplicate_diagnostics() => _snapshot.Compilation.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_not_choose_between_the_duplicates() => _snapshot.Index.Resolve(_snapshot.Index.References.Single(reference => reference.Role == "produces")).Length.ShouldEqual(2);
}
