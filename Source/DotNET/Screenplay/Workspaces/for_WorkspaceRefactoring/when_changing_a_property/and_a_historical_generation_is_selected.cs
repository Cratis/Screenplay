// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class and_a_historical_generation_is_selected : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request() with { NewGeneration = true, AlsoRename = [Select("Registered"), Select("Audited", 1)] });

    [Fact] void should_refuse_historical_mutation() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.HistoricalGenerationImmutable);
    [Fact] void should_leave_every_event_and_identity_untouched() => Result.Workspace.ShouldBeNull();
    [Fact] void should_have_no_write_plan() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_have_no_accepted_generations() => Result.PropertyReport.Generations.ShouldBeEmpty();
}
