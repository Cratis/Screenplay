// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_appending_an_absence_assertion_through_authoring : Specification
{
    const string Source =
        """
        module Shop
          feature Orders
            slice StateView Lookup
              readmodel InvoiceView
                invoiceId String
              query InvoiceById => InvoiceView?
                by invoiceId String
              specification NoInvoice
                then no readmodel InvoiceView for "first"
        """;

    WorkspaceAuthoringResult _result = null!;
    WorkspaceAuthoringResult _prepended = null!;

    void Because()
    {
        var document = WorkspaceDocument.Create("orders", PortablePlayPath.Parse("Shop/Orders.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(candidate => candidate.Node is SpecificationSyntax);
        var specification = (SpecificationSyntax)entry.Node;
        var appended = specification.ThenAbsentReadModels.Single() with
        {
            Key = new Cratis.Screenplay.Syntax.LiteralExpressionSyntax("second", SourceLocation.Start),
            Location = SourceLocation.Start
        };
        _result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new AddWorkspaceNode(entry.Handle, entry.Node, "thenAbsentReadModels", appended)]
        });
        _prepended = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new AddWorkspaceNode(entry.Handle, entry.Node, "thenAbsentReadModels", appended, 0)]
        });
    }

    [Fact] void should_refuse_the_append_without_absence_binding_proof() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_the_prepend_without_occurrence_provenance() => _prepended.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_write_plan_for_either_proposal()
    {
        _result.WritePlan.ShouldBeNull();
        _prepended.WritePlan.ShouldBeNull();
    }
}
