// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_mixed_event_generation_pins;

public class and_a_pin_is_a_conflicting_earlier_identity : given.a_workspace_with_mixed_event_generation_pins
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = Create(Source.Replace("id \"Renamed\"", "id \"Earlier\"", StringComparison.Ordinal));

    void Because() => _result = Workspace.ProposeRename(Rename("Renamed", "Again"));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_return_a_typed_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_explain_the_identity_disagreement() => _result.Conflicts.Single().Message.ShouldContain("contradictory effective identity pins");
    [Fact] void should_not_produce_a_candidate_workspace() => _result.Workspace.ShouldBeNull();
    [Fact] void should_not_produce_writes() => _result.WritePlan.ShouldBeNull();
}
