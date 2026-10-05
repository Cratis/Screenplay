// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_mixed_event_generation_pins;

public class and_the_event_was_never_persisted : given.a_workspace_with_mixed_event_generation_pins
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeRename(Rename("Renamed", "Again", true));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_every_generation() => Events(_result.Workspace!).Select(declaration => declaration.Name).ShouldEqual(["Again", "Again"]);
    [Fact] void should_remove_all_pins() => Events(_result.Workspace!).All(declaration => declaration.Id is null).ShouldBeTrue();
    [Fact] void should_remove_only_the_whole_pin_line_and_rename_the_headers() => _result.Workspace!.Documents[0].Text.ShouldEqual(Source.Replace("        id \"Renamed\"\n", "", StringComparison.Ordinal).Replace("event Renamed", "event Again", StringComparison.Ordinal));
}
