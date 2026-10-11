// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_an_inserted_mapping_in_a_source_specific_handler : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(SharedSource.Replace("other String", "label String", StringComparison.Ordinal).Replace("from A, B", "from A\n        from B", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("A") with { EventNeverPersisted = true });

    [Fact] void should_preserve_the_independent_feed() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_only_the_proven_source_specific_mapping() => Candidate.Split("name = label", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_keep_b_unselected() => Result.PropertyReport.Generations.Single().Event.Name.ShouldEqual("A");
}
