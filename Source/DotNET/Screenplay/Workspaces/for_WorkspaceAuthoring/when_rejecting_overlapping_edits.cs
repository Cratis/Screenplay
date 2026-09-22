// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_overlapping_edits : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var concept = Index.Entries.First(entry => entry.Node is ConceptSyntax);
        _result = Workspace.ProposeAuthoring(Authoring(
            new ReplaceWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, ConceptsRoot.Node),
            new RemoveWorkspaceNode(concept.Handle, concept.Node)));
    }

    [Fact] void should_reject_parent_descendant_overlap() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_expose_partial_writes() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_report_an_operation_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
}
