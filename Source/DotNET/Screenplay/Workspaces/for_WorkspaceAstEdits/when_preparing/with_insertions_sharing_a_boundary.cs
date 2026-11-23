// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAstEdits.when_preparing;

public class with_insertions_sharing_a_boundary : given.an_ast_edit_workspace
{
    Exception?[] _errors = [];

    void Establish()
    {
        var concepts = Index.Entries.Where(entry => entry.Node is ConceptSyntax).ToArray();
        Operations =
        [
            new AddWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, "concepts", concepts[0].Node, 1),
            new AddWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, "concepts", concepts[1].Node, 1)
        ];
    }

    void Because() => _errors =
    [
        Catch.Exception(() => new WorkspaceAstEdits(Index).Prepare([.. Operations])),
        Catch.Exception(() => new WorkspaceAstEdits(Index).Prepare([.. Operations.Reverse()]))
    ];

    [Fact] void should_reject_both_operation_orders() => _errors.ShouldEachConformTo(error => error is InvalidWorkspaceAuthoring);
    [Fact] void should_preserve_the_boundary_conflict_message() => _errors.Select(error => error!.Message).ShouldEqual(Enumerable.Repeat("Multiple insertions claim the same original member and insertion boundary.", 2));
}
