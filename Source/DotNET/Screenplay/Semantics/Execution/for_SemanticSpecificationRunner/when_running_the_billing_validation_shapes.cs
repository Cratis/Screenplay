// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// The Stage Billing sample's 'amount > 0' and concept 'length == 10' rules decide its specifications end to end.
public class when_running_the_billing_validation_shapes : Specification
{
    const string Source =
        """
        concept InvoiceId : Uuid
        concept InvoiceNumber : String
          validate
            not empty message "An invoice needs a number"
            length == 10
        concept Money : Decimal
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                invoiceNumber InvoiceNumber
                amount Money
                validate
                  amount > 0 message "An invoice for nothing is not an invoice"
                produces InvoiceRegistered
                  for invoiceId
                  invoiceId = invoiceId
                  invoiceNumber = invoiceNumber
                  amount = amount
              event InvoiceRegistered
                invoiceId InvoiceId
                invoiceNumber InvoiceNumber
                amount Money
              specification RegisteringAnInvoice
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000123"
                  amount = 1500
                then InvoiceRegistered
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000123"
                  amount = 1500
              specification RejectingAnInvoiceForNothing
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000124"
                  amount = 0
                then error "An invoice for nothing is not an invoice"
              specification RejectingAShortInvoiceNumber
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-1"
                  amount = 1500
                then error "A value must be exactly 10 characters long."
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun[] _runs;

    void Establish()
    {
        const string StableKey = "billing-validation";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "RegisterInvoice.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _runs = [.. _plan.Specifications.Values.Select(_ => runner.Run(_plan, _.Id))];
    }

    [Fact] void should_run_every_specification() => _runs.Length.ShouldEqual(3);
    [Fact] void should_pass_every_specification() => _runs.All(_ => _.Passed).ShouldBeTrue();
    [Fact] void should_reject_twice() => _runs.Count(_ => _.Execution is SemanticRejected).ShouldEqual(2);
    [Fact] void should_accept_once() => _runs.Count(_ => _.Execution is SemanticAccepted).ShouldEqual(1);
}
