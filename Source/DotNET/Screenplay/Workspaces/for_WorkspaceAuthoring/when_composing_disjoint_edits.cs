// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_composing_disjoint_edits : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceAstOperation[] _operations = [];

    void Establish()
    {
        _operations = [.. Index.Entries.Where(entry => entry.Node is ConceptSyntax).Select(entry =>
            (WorkspaceAstOperation)new ReplaceWorkspaceNode(entry.Handle, entry.Node, (ConceptSyntax)entry.Node with
            {
                File = new FileReferenceSyntax($"Concepts/{((ConceptSyntax)entry.Node).Name}.cs", SourceLocation.Start)
            }))];
    }

    void Because() => _result = Workspace.ProposeAuthoring(Authoring(_operations));

    [Fact] void should_accept_both_edits_to_one_document() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_plan_only_one_document_write() => _result.WritePlan.Entries.Length.ShouldEqual(1);
    [Fact] void should_retain_both_requested_fields() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Count(entry => entry.Node is FileReferenceSyntax).ShouldEqual(2);
    [Fact] void should_be_independent_of_operation_order() => Workspace.ProposeAuthoring(Authoring([.. _operations.Reverse()])).Workspace.Revision.ShouldEqual(_result.Workspace.Revision);
}
