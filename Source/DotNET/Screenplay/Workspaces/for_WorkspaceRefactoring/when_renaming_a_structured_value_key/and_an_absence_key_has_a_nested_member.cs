// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_an_absence_key_has_a_nested_member : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("part", "segment"));

    [Fact] void should_accept_the_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_produce_a_write_plan() => _result.WritePlan.ShouldNotBeNull();
    [Fact] void should_rename_the_declaration() => Text(_result).ShouldContain("  segment String");
    [Fact] void should_rewrite_the_bound_absence_key_member() => Text(_result).ShouldContain("{\"id\":\"first\",\"detail\":{\"segment\":\"old\"}}");
    [Fact] void should_leave_the_original_workspace_unchanged() => Workspace.Documents.Single().Text.ShouldContain("\"detail\":{\"part\":\"old\"}");
}
