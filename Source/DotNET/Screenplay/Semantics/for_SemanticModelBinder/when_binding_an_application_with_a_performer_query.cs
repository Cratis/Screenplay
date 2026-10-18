// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_application_with_a_performer_query : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String
              query InvoiceById => InvoiceView?
                by invoiceId String
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
              specification RemovingAnInvoice
                given readmodel InvoiceView
                  invoiceId = "first"
                when append InvoiceRemoved
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                total Int
              query CurrentFigures => InvoiceFigures?
                by total Int
                performer
                  file Queries/CurrentFigures.cs
              specification ReadingTheFigures
                given readmodel InvoiceFigures
                  total = 3
                then query CurrentFigures
                  total = 3
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_report_the_performer_query_as_an_opaque_read() => _result.Diagnostics.ShouldContain(_ => _.Severity == DiagnosticSeverity.Information && _.Message.Contains("Query 'CurrentFigures' is performed by code", StringComparison.Ordinal));
    [Fact] void should_still_list_the_performer_as_code_the_model_needs() => _result.ImplementationRequirements.Select(value => value.Role).ShouldContain(SemanticImplementationRole.QueryPerformer);
    [Fact] void should_keep_the_declarative_specification_executable()
    {
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        plan.Specifications.Values.Select(_ => _.Name).ShouldContain("RemovingAnInvoice");
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(_ => _.Name == "RemovingAnInvoice").Id).Passed.ShouldBeTrue();
    }
    [Fact] void should_leave_out_the_specification_that_reads_the_opaque_query() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Specifications).Select(_ => _.Name).ShouldNotContain("ReadingTheFigures");
}
