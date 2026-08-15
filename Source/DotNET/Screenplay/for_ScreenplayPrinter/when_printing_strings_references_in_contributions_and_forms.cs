// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_strings_references_in_contributions_and_forms : given.a_printer
{
    const string Source =
        """
        module Invoicing
          layout AppShell
            template
              navbar contributes Navigation
              main

          contribute to Navigation
            navigate to InvoiceList
            label $strings.invoices.nav.label

          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

            slice StateView InvoiceList
              screen InvoiceList

          form RegisterInvoiceForm for RegisterInvoice
            field invoiceId label $strings.invoices.fields.invoiceId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_contribution_label_unquoted() => _roundtrip.Printed.ShouldContain("label $strings.invoices.nav.label");
    [Fact] void should_print_the_form_field_label_unquoted() => _roundtrip.Printed.ShouldContain("label $strings.invoices.fields.invoiceId");
    [Fact] void should_store_the_contribution_label_as_the_strings_reference() =>
        _roundtrip.Reparsed.Value!.Modules.Single().Contributions!.Single().Label.ShouldEqual("$strings.invoices.nav.label");
    [Fact] void should_store_the_form_field_label_as_the_strings_reference() =>
        _roundtrip.Reparsed.Value!.Modules.Single().Forms!.Single().Fields.Single().Label.ShouldEqual("$strings.invoices.fields.invoiceId");
}
