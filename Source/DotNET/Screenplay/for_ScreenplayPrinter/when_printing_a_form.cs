// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_form : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
                customerName String
                dueDate Date
                totalAmount Decimal
                lineItems String[]

            slice StateView InvoiceDraft
              query GetInvoiceDraft => InvoiceDraftReadModel

            slice StateView InvoiceList
              screen InvoiceList

          form RegisterInvoiceForm for RegisterInvoice
            populate via query GetInvoiceDraft by invoiceId

            field customerName
            field dueDate label "Due date"
            field totalAmount from calculatedTotal
            field lineItems compose using BuildLineItems

            on submit navigate to InvoiceList by invoiceId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_form_header() => _roundtrip.Printed.ShouldContain("form RegisterInvoiceForm for RegisterInvoice");
    [Fact] void should_print_the_populate_declaration() => _roundtrip.Printed.ShouldContain("populate via query GetInvoiceDraft by invoiceId");
    [Fact] void should_print_the_bare_field() => _roundtrip.Printed.ShouldContain("field customerName\n");
    [Fact] void should_print_the_labeled_field() => _roundtrip.Printed.ShouldContain("field dueDate label \"Due date\"");
    [Fact] void should_print_the_renamed_field() => _roundtrip.Printed.ShouldContain("field totalAmount from calculatedTotal");
    [Fact] void should_print_the_composed_field() => _roundtrip.Printed.ShouldContain("field lineItems compose using BuildLineItems");
    [Fact] void should_print_the_submit_navigation() => _roundtrip.Printed.ShouldContain("on submit navigate to InvoiceList by invoiceId");
    [Fact] void should_preserve_all_fields() => Form.Fields.Count().ShouldEqual(4);

    FormSyntax Form => _roundtrip.Reparsed.Value!.Modules.Single().Forms!.Single();
}
