// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_two_inline_events_are_selected : given.an_identifier_payload_copy_workspace
{
    const string Other = "    slice StateChange Archive\n      command Archive\n        projectId Uuid identifier\n        produces event Archived\n          projectId Uuid = projectId\n";

    void Establish() => Create(InlineSource + Other);

    void Because() => Result = Workspace.ProposeProperty(Request() with
    {
        AlsoRemove = [new(Property(Workspace, "projectId", eventName: "Archived").Handle, "projectId")]
    });

    [Fact] void should_refuse() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_name_both_events() => Result.PropertyReport!.NeedsDecision.Single().Candidates.Length.ShouldEqual(2);
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}
