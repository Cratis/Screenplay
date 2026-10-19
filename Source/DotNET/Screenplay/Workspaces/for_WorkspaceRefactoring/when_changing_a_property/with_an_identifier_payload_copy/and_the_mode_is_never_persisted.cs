// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_the_mode_is_never_persisted : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource);

    void Because() => Result = Workspace.ProposeProperty(Request() with { NewGeneration = false, EventNeverPersisted = true });

    [Fact] void should_require_explicit_extraction() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InlineEventRequiresExtraction);
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}
