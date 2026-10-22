// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_a_specification_also_covers_a_staying_declaration : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var source = Model().Replace(
            "  feature Destination",
            """
                  specification FindingProjects
                    given ProjectRegistered
                      for "0f8fad5b-d9cb-469f-a165-70867728950e"
                      name = "A"
                    when query AllProjects
                    then result
                      projectId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                      name = "A"
              feature Destination
            """,
            StringComparison.Ordinal);
        Workspace = ScreenplayWorkspace.Create("Projects", [Document("model", "application.play", source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        Assert.True(Workspace.Compilation.Success, CompilationDiagnostics(Workspace));
    }

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForQuery(Slice("Source", "Listing"), "AllProjects"), Slice("Destination", "Lookup")));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_covering_specification() => Refusals(_result).ShouldContain("Specification 'Projects.Source.Listing.FindingProjects'");
    [Fact] void should_name_the_staying_declaration() => Refusals(_result).ShouldContain("ReadModel 'Projects.Source.Listing.ProjectView'");
    [Fact] void should_return_no_candidate() => _result.Workspace.ShouldBeNull();
}
