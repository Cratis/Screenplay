// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_rejecting_a_type_collision : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "String"));

    [Fact] void should_reject_primitive_shadowing() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_expose_a_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_not_expose_a_write_plan() => _result.WritePlan.ShouldBeNull();
}
