// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_storyline_targets;

public class and_the_target_is_a_slice_or_container : given.a_storyline_workspace
{
    WorkspaceAuthoringResult _feature = null!;
    WorkspaceAuthoringResult _module = null!;

    void Because()
    {
        Result = Workspace.ProposeRename(Rename<SliceSyntax>("RequestReturn", "AskForReturn"));
        _feature = Workspace.ProposeRename(Rename<FeatureSyntax>("Requests", "Asks"));
        _module = Workspace.ProposeRename(Rename<ModuleSyntax>("Returns", "Refunds"));
    }

    [Fact] void should_accept_the_slice_rename() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_slice_step() => Text.ShouldContain("  slice Requests.AskForReturn\n");
    [Fact] void should_leave_the_command_step_alone() => Text.ShouldContain("  command RequestReturn \"Asks\"");
    [Fact] void should_accept_the_feature_rename() => _feature.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_feature_and_its_qualified_slice_step() => _feature.Workspace.Documents.Single().Text.ShouldContain("  feature Returns.Asks\n  slice Asks.RequestReturn\n");
    [Fact] void should_accept_the_module_rename() => _module.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_module_steps() => _module.Workspace.Documents.Single().Text.ShouldContain("  module Refunds\n  feature Refunds.Requests\n");
}
