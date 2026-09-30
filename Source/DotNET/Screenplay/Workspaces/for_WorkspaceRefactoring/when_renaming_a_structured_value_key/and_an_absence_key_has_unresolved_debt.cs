// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_an_absence_key_has_unresolved_debt : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal));

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("part", "segment"));

    [Fact] void should_refuse_a_rename_that_could_capture_the_debt() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_produce_a_write_plan() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_name_the_unresolved_key() => _result.Conflicts.Single().Message.ShouldContain("absence key 'segment'");
}
