// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_asserting_null_for_an_optional_property : a_semantic_binder
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRegistered
                invoiceId Uuid
              readmodel InvoiceSummary
                invoiceId Uuid
                status String?
              query InvoiceById => InvoiceSummary?
                by invoiceId Uuid
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceRegistered key invoiceId
              specification LookingUpAnInvoice
                given InvoiceRegistered
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                then readmodel InvoiceSummary
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  status = null
        """;

    SemanticSpecificationRun _result;

    void Because()
    {
        var model = Bind(Source).Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
    }

    [Fact] void should_match_the_projection_initialized_optional_null() => _result.Passed.ShouldBeTrue();
}
