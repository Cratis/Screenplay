// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_property_with_an_unknown_type : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
                lines     InvoiceLine[]

              event InvoiceRegistered
                lines InvoiceLine[]

            slice StateView InvoiceList
              query ListInvoices => InvoiceListReadModel[]
                filter status InvoiceStatus?
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_for_every_reference() => _result.Diagnostics.Count().ShouldEqual(3);
    [Fact] void should_warn_about_the_event_property() => _result.Diagnostics.First().Message.ShouldEqual("Unknown type 'InvoiceLine' on 'lines' of event 'InvoiceRegistered' - declare it with 'concept InvoiceLine : <Primitive>' or 'type InvoiceLine'");
    [Fact] void should_warn_about_the_command_property() => _result.Diagnostics.Skip(1).First().Message.ShouldEqual("Unknown type 'InvoiceLine' on 'lines' of command 'RegisterInvoice' - declare it with 'concept InvoiceLine : <Primitive>' or 'type InvoiceLine'");
    [Fact] void should_warn_about_the_query_parameter() => _result.Diagnostics.Last().Message.ShouldEqual("Unknown type 'InvoiceStatus' on 'status' of query 'ListInvoices' - declare it with 'concept InvoiceStatus : <Primitive>' or 'type InvoiceStatus'");
    [Fact] void should_not_warn_about_the_return_type() => _result.Diagnostics.Any(_ => _.Message.Contains("InvoiceListReadModel", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_report_warnings_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
