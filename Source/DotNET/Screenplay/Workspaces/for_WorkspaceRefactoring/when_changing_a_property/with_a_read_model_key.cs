// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_a_read_model_key : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Project") with { Target = Property("Project", "projectId").Handle, ExpectedName = "projectId" });

    [Fact] void should_refuse_key_inference_changes() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedPropertySpan);
}
