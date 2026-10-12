// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_optional_values_in_case_examples : given.a_case_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CaseRequest() with { Optional = true, Validation = WorkspaceAuthoringValidation.Authoring });

    [Fact] void should_accept_the_explicit_event_choice() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_not_add_example_or_assertion_values() => Result.PropertyReport.Changed.Any(change => change.Change == "fixtureAdded").ShouldBeFalse();
}
