// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_a_move_into_itself : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var feature = Index.Entries.Single(entry => entry.Node is FeatureSyntax);
        _result = Workspace.ProposeAuthoring(Authoring(new MoveWorkspaceNode(feature.Handle, feature.Node, feature.Handle, feature.Node, "features")));
    }

    [Fact] void should_reject_self_ownership() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_expose_no_candidate() => _result.Workspace.ShouldBeNull();
}
