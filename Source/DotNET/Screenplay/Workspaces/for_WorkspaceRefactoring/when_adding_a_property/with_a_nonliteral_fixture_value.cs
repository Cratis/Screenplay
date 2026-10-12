// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_nonliteral_fixture_value : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { FixtureValue = "priority" });

    [Fact] void should_require_literal_source_text() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedPropertySpan);
    [Fact] void should_keep_declaration_edits_unpublished() => Result.PropertyReport.Changed.ShouldBeEmpty();
}
