// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_implicit_formatting : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(new ReplaceWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, ConceptsRoot.Node)) with
    {
        Formatting = WorkspaceAuthoringFormatting.PreserveExactSource
    });

    [Fact] void should_require_explicit_normalization_permission() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_rewrite_source() => _result.WritePlan.ShouldBeNull();
}
