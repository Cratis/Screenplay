// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_mixed_event_generation_pins;

public class and_default_pinning_is_used : given.a_workspace_with_mixed_event_generation_pins
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeRename(Rename("Renamed", "Again"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_every_generation() => Events(_result.Workspace!).Select(declaration => declaration.Name).ShouldEqual(["Again", "Again"]);
    [Fact] void should_pin_every_generation_to_the_old_name() => Events(_result.Workspace!).Select(declaration => declaration.Id).ShouldEqual(["Renamed", "Renamed"]);
    [Fact] void should_preserve_all_comments() => _result.Workspace!.Documents[0].Text.ShouldContain("// first generation\n        id \"Renamed\"\n        name   String // property intent\n      event Again generation 2 // second generation\n");
}
