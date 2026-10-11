// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_an_inserted_mapping_missing_a_shared_source : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(SharedSource);

    void Because() => Result = Workspace.ProposeProperty(Request("View"));

    [Fact] void should_refuse_unselected_projection_effect_changes() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_leave_source_and_identity_changes_atomic() => Result.Workspace.ShouldBeNull();
    [Fact] void should_report_no_accepted_event_evolutions() => Result.PropertyReport.Generations.ShouldBeEmpty();
}
