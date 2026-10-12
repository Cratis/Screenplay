// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_compatible_selected_shared_automap_feeds : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(SharedSource.Replace("other String", "name String", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("A") with { AlsoRename = [Select("B")], NewGeneration = true });

    [Fact] void should_preserve_every_proven_shared_feed() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_one_equivalent_mapping() => Candidate.Split("name = label", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_report_both_selected_evolutions() => Result.PropertyReport.Generations.Length.ShouldEqual(2);
}
