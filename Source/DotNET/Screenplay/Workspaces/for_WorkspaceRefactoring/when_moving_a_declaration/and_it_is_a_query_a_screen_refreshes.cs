// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_is_a_query_a_screen_refreshes : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForQuery(Slice("Source", "Listing"), "AllProjects"), Slice("Destination", "Lookup")));

    [Fact] void should_accept() => Assert.True(_result.Accepted, Refusals(_result));
    [Fact] void should_place_the_query_in_the_destination_slice() => SliceNamed(_result, "Lookup").Queries.Select(query => query.Name).ShouldContainOnly("AllProjects");
    [Fact] void should_repair_the_refresh_target() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<RefreshQueryActionSyntax>().Single().Query.ShouldEqual("Projects.Destination.Lookup.AllProjects");
    [Fact] void should_report_the_repair() => _result.MoveReport!.ReferenceRepairs.Select(repair => repair.Current).ShouldContainOnly("Projects.Destination.Lookup.AllProjects");
}
