// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_workspace_with_an_absent_read_model_key : a_refactoring_workspace
{
    protected const string Source =
        """
        type InvoicePart
          part String
        type InvoiceKey
          id String
          detail InvoicePart
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              readmodel InvoiceView
                invoiceId InvoiceKey
              query InvoiceById => InvoiceView?
                by invoiceId InvoiceKey
              specification NoInvoice
                then no readmodel InvoiceView for {"id":"first","detail":{"part":"old"}}
        """;

    protected const string Assertion = "then no readmodel InvoiceView for {\"id\":\"first\",\"detail\":{\"part\":\"old\"}}";

    protected WorkspaceDocument Invoice = null!;

    void Establish() => CreateWith(Source);

    protected void CreateWith(string source)
    {
        Invoice = Document("invoice", "Billing/Invoices.play", source);
        Workspace = ScreenplayWorkspace.Create("Billing", [Invoice], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
    }

    protected WorkspaceSyntaxEntry Entry<T>(Func<T, bool> matches)
        where T : SyntaxNode => WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is T node && matches(node));

    protected WorkspaceSyntaxEntry Member(string name) => Entry<ObjectMemberSyntax>(member => member.Name == name);

    protected ReplaceWorkspaceNode RenameMember(string name, string newName)
    {
        var entry = Member(name);
        return new(entry.Handle, entry.Node, ((ObjectMemberSyntax)entry.Node) with { Name = newName });
    }

    protected AddWorkspaceNode PrependAssertion()
    {
        var assertion = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is SpecificationAbsentReadModelSyntax);
        var specification = WorkspaceSyntaxIndex.Create(Workspace).Find(assertion.Parent!)!;
        return new(specification.Handle, specification.Node, "thenAbsentReadModels", assertion.Node, 0);
    }

    protected WorkspaceAuthoringRequest Authoring(WorkspaceAuthoringReferencePolicy policy, params WorkspaceAstOperation[] operations) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        ReferencePolicy = policy,
        Operations = [.. operations]
    };

    protected WorkspaceAuthoringRequest ReplaceDocument(WorkspaceAuthoringReferencePolicy policy, string source) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        ReferencePolicy = policy,
        Documents = [new ReplaceWorkspaceSyntaxDocument(Invoice.Id, new ScreenplayCompiler().Parse(source, "Billing/Invoices.play").Value!)]
    };

    protected static string Text(WorkspaceAuthoringResult result) => result.Workspace!.Documents.Single().Text;

    protected static IEnumerable<string> AbsenceDebt(WorkspaceAuthoringResult result) =>
        result.AuthoringDiagnostics.Where(diagnostic => diagnostic.Message.Contains("unresolved absence key", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Message);
}
