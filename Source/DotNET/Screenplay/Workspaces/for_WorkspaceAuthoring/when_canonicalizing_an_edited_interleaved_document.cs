// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_canonicalizing_an_edited_interleaved_document : given.a_document_with_literal_values
{
    void Establish() => Create(OrderSource + "\n" +
        """
              constraint UniqueOrder
                unique event OrderPlaced
            slice StateView List
              query Find => OrderId[]
              screen List
                table Find
                  column orderId
              query Other => OrderId[]
        """);

    void Because()
    {
        var entry = Value("remark");
        Result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Operations = [new ReplaceWorkspaceNode(
                entry.Handle,
                entry.Node,
                ((PropertyMappingSyntax)entry.Node) with { Source = new LiteralExpressionSyntax("sturdy", SourceLocation.Start) })]
        });
    }

    [Fact] void should_accept_the_proposal() => Result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_constraint_after_specifications() => InOrder("specification PlacingAnOrder", "constraint UniqueOrder");
    [Fact] void should_keep_screen_between_queries() => InOrder("query Find", "screen List", "query Other");
    [Fact] void should_print_the_edit() => Result.Workspace!.Documents.Single().Text.ShouldContain("remark = \"sturdy\"");

    void InOrder(params string[] declarations)
    {
        var text = Result.Workspace!.Documents.Single().Text;
        var positions = declarations.Select(declaration => text.IndexOf(declaration, StringComparison.Ordinal)).ToArray();
        positions.All(position => position >= 0).ShouldBeTrue();
        positions.SequenceEqual(positions.Order()).ShouldBeTrue();
    }
}
