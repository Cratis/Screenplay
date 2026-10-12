// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_all_shared_variant_targets_selected_for_rename : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(VariantSource.Replace("\n        label String optional", string.Empty, StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("One", name: "old") with { AlsoRename = [new(Property("Two", "old").Handle, "old")] });

    [Fact] void should_accept_the_compatible_explicit_selection() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_retarget_the_shared_mapping_once() => Candidate.Split("label = name", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_not_change_an_event_contract() => Result.PropertyReport.Generations.ShouldBeEmpty();
}
