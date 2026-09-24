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
    [Fact] void should_report_no_dropped_comments() => _dropped.ShouldBeEmpty();
    [Fact] void should_retain_every_comment_in_the_printed_document() => Result.WritePlan!.Entries.Single().After!.Text.ShouldContain("// @public command PlaceOrder");
    [Fact] void should_state_zero_dropped_comments_in_the_normalization_warning() => Result.AuthoringDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).Message.ShouldContain("dropped no comments");
}
