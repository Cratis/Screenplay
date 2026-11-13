// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_storyline_targets;

public class and_the_target_is_a_command_or_event : given.a_storyline_workspace
{
    WorkspaceAuthoringResult _event = null!;

    void Because()
    {
        Result = Workspace.ProposeRename(Rename<CommandSyntax>("RequestReturn", "AskForReturn"));
        _event = Workspace.ProposeRename(Rename<EventSyntax>("ReturnRequested", "ReturnAsked"));
    }

    [Fact] void should_have_no_conflicts() => Result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_accept_the_command_rename() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_command_step() => Text.ShouldContain("  command AskForReturn \"Asks\"");
    [Fact] void should_leave_the_slice_step_alone() => Text.ShouldContain("  slice Requests.RequestReturn");
    [Fact] void should_accept_the_event_rename() => _event.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_event_step() => _event.Workspace.Documents.Single().Text.ShouldContain("  event ReturnAsked\n");
}
