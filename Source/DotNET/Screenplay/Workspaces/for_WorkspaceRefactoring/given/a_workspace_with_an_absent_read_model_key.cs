// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

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

    protected WorkspaceDocument Invoice = null!;

    void Establish() => CreateWith(Source);

    protected void CreateWith(string source)
    {
        Invoice = Document("invoice", "Billing/Invoices.play", source);
        Workspace = ScreenplayWorkspace.Create("Billing", [Invoice], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
    }
}
