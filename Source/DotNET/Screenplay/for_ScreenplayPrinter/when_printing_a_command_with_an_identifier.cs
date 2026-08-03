// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_command_with_an_identifier : given.a_printer
{
    const string Source =
        """
        concept InvoiceId     : Uuid
        concept InvoiceNumber : String

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId     InvoiceId identifier
                invoiceNumber InvoiceNumber
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_modifier() => _roundtrip.Printed.ShouldContain("invoiceId InvoiceId identifier");
    [Fact] void should_leave_the_other_property_bare() => _roundtrip.Printed.ShouldContain("invoiceNumber InvoiceNumber\n");
    [Fact] void should_preserve_the_identifier() => Command.Properties.Single(_ => _.IsIdentifier).Name.ShouldEqual("invoiceId");

    CommandSyntax Command => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
