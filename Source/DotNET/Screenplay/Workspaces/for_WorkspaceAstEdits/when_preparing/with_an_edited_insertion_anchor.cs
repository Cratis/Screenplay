// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAstEdits.when_preparing;

public class with_an_edited_insertion_anchor : given.an_ast_edit_workspace
{
    Exception?[] _errors = [];

    void Establish()
    {
        var concept = Index.Entries.First(entry => entry.Node is ConceptSyntax);
        Operations =
        [
            new RemoveWorkspaceNode(concept.Handle, concept.Node),
            new AddWorkspaceNode(ConceptsRoot.Handle, ConceptsRoot.Node, "concepts", concept.Node, 0)
        ];
    }

    void Because() => _errors =
    [
        Catch.Exception(() => new WorkspaceAstEdits(Index).Prepare([.. Operations])),
        Catch.Exception(() => new WorkspaceAstEdits(Index).Prepare([.. Operations.Reverse()]))
    ];

    [Fact] void should_reject_both_operation_orders() => _errors.ShouldEachConformTo(error => error is InvalidWorkspaceAuthoring);
    [Fact] void should_preserve_the_anchor_conflict_message() => _errors.Select(error => error!.Message).ShouldEqual(Enumerable.Repeat("An original insertion anchor is also removed, replaced, or moved.", 2));
}
