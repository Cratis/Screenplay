// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// A document that names no file prints exactly as it did before the directive reached these declarations,
/// down to the byte. The expected text is written out in full rather than asserted piecewise, so a stray line
/// or an escape the printer did not use to add is a failure rather than something nobody looked at.
/// </summary>
/// <remarks>
/// The properties named <c>file</c> are the point of the fixture. In a block that also reads property lines
/// the directive is told from a property by shape, and the property wins the tie - so <c>file Attachment</c>
/// still declares a property and still prints unescaped, while the trigger, whose body has always reserved the
/// word, still escapes it.
/// </remarks>
public class when_printing_declarations_that_name_no_file : given.a_printer
{
    const string Source =
        """
        concept Attachment : String

        concept InvoiceId : Uuid

        type InvoiceLine
          file Attachment
          amount Decimal

        trigger LedgerFileArrived
          description "A file arrived from the ledger"
          @file Attachment
          name

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              description "Registers an invoice"

              event InvoiceRegistered
                file Attachment
                invoiceId InvoiceId

              readmodel Invoice
                file Attachment

              projection Invoices => Invoice
                from InvoiceRegistered
                  invoiceId = invoiceId
        """;

    const string Expected =
        """
        concept Attachment : String

        concept InvoiceId : Uuid

        type InvoiceLine
          file Attachment
          amount Decimal

        trigger LedgerFileArrived
          description "A file arrived from the ledger"
          @file Attachment
          name

        module Invoicing

          feature Invoices

            slice StateChange RegisterInvoice
              description "Registers an invoice"

              event InvoiceRegistered
                file Attachment
                invoiceId InvoiceId

              readmodel Invoice
                file Attachment

              projection Invoices => Invoice
                from InvoiceRegistered
                  invoiceId = invoiceId

        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_what_it_always_printed() => _roundtrip.Printed.ShouldEqual(Expected.ReplaceLineEndings("\n"));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_leave_every_declaration_without_a_file_reference() => Files().ShouldBeEmpty();

    IEnumerable<FileReferenceSyntax> Files()
    {
        var walker = new Syntax.for_ScreenplaySyntaxWalker.given.a_counting_walker();
        walker.VisitApplication(_roundtrip.Reparsed.Value!);
        return walker.Nodes.OfType<FileReferenceSyntax>();
    }
}
