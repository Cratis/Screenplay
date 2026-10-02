// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_mixed_event_generations_again;

public class and_the_first_rename_pinned_the_old_name : given.a_workspace_with_mixed_event_generation_pins
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var first = Workspace.ProposeRename(Rename("Renamed", "Again"));
        first.Accepted.ShouldBeTrue();
        Workspace = first.Workspace!;
    }

    void Because() => _result = Workspace.ProposeRename(Rename("Again", "Final", true));

    [Fact] void should_accept_the_subsequent_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_every_generation_again() => Events(_result.Workspace!).Select(declaration => declaration.Name).ShouldEqual(["Final", "Final"]);
    [Fact] void should_preserve_the_genuine_earlier_identity() => Events(_result.Workspace!).Select(declaration => declaration.Id).ShouldEqual(["Renamed", "Renamed"]);
}
