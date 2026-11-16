// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// A provided value computed by code (ESM v57) and the availability gate beside it are decided by the host. Specifications
// always run with everything available, so the provide neither denies nor refuses the slice's specification.
public class when_a_code_provide_gates_a_slice : a_semantic_binder
{
    const string Source =
        """
        audience Plus

        module Invoicing
          feature Invoices
            slice StateView InvoiceLookup
              available for Plus
              provide ceiling Int
                ```csharp
                return 50000;
                ```
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

    CompilationResult<SemanticCompilation> _bound;
    SemanticSpecificationRun _result;

    void Because()
    {
        _bound = Bind(Source);
        var plan = SemanticExecutionPlan.Compile(_bound.Value!.Model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
    }

    [Fact] void should_bind_the_code_provide() => _bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V57);
    [Fact] void should_run_the_specification_unchanged() => _result.Passed.ShouldBeTrue();
}
