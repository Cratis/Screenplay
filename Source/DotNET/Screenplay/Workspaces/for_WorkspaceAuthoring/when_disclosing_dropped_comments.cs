// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_disclosing_dropped_comments : given.a_document_with_literal_values
{
    WorkspaceDroppedComment[] _dropped = [];

    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        var produces = (ProducesSyntax)entry.Node;
        Result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, produces with { Mappings = [.. produces.Mappings.Reverse()] })]
        });
        _dropped = [.. WorkspaceDroppedComments.In(Result.WritePlan!)];
    }

    [Fact] void should_accept_the_canonical_edit() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_list_every_dropped_comment() => _dropped.Select(comment => comment.Line).ShouldContainOnly(1, 7, 26, 30);
    [Fact] void should_keep_the_exact_comment_text() => _dropped[1].Text.ShouldEqual("// @public command PlaceOrder");
    [Fact] void should_name_the_original_path() => _dropped.All(comment => comment.Path == Order.Path).ShouldBeTrue();
    [Fact] void should_state_the_count_in_the_normalization_warning() => Result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Message.Contains("dropped 4 comments (lines 1, 7, 26, 30)", StringComparison.Ordinal).ShouldBeTrue();
}
