// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_invalid_slots : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _unknown = null!;
    WorkspaceAuthoringResult _wrongType = null!;

    void Because()
    {
        var node = new ImportSyntax("External.Unused", SourceLocation.Start);
        _unknown = Workspace.ProposeAuthoring(Authoring(new AddWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, "missing", node)));
        _wrongType = Workspace.ProposeAuthoring(Authoring(new AddWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, "concepts", node)));
    }

    [Fact] void should_reject_unknown_members() => _unknown.Accepted.ShouldBeFalse();
    [Fact] void should_reject_wrong_typed_nodes() => _wrongType.Accepted.ShouldBeFalse();
    [Fact] void should_never_plan_partial_writes() => new[] { _unknown, _wrongType }.All(result => result.WritePlan is null).ShouldBeTrue();
}
