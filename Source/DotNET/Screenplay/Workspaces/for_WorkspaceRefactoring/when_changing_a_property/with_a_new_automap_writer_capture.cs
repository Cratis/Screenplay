// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_a_new_automap_writer_capture : given.a_multi_source_property_workspace
{
    void Establish() => UseBindableSource(PotentialAutoMapSource);

    void Because() => Result = Workspace.ProposeProperty(Request("A", name: "old") with { NewName = "name", EventNeverPersisted = true });

    [Fact] void should_refuse_the_newly_introduced_writer() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_not_expose_a_changed_event_or_identity() => Result.Workspace.ShouldBeNull();
    [Fact] void should_report_no_accepted_event_edits() => Result.PropertyReport.Generations.ShouldBeEmpty();
}
