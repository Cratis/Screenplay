// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_historical_event_owner : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Audited", 1) with { EventNeverPersisted = true, Optional = true });

    [Fact] void should_protect_the_stored_generation() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.HistoricalGenerationImmutable);
    [Fact] void should_not_report_an_evolution() => Result.PropertyReport.Generations.ShouldBeEmpty();
}
