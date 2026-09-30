// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_repairing_a_leaf_member : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace("\"part\":\"old\"", "\"missing\":\"old\"", StringComparison.Ordinal));

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(WorkspaceAuthoringReferencePolicy.Safe, RenameMember("missing", "part")) with
    {
        Formatting = WorkspaceAuthoringFormatting.PreserveTrivia
    });

    [Fact] void should_accept_the_explicit_repair() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_supply_a_write_plan() => _result.WritePlan.ShouldNotBeNull();
    [Fact] void should_write_the_repaired_key() => Text(_result).ShouldContain(Assertion);
    [Fact] void should_leave_no_absence_debt() => AbsenceDebt(_result).ShouldBeEmpty();
}
