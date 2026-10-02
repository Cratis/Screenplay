// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_specification_clocks_triggers_captures_and_queries : given.a_printer
{
    const string Source =
        """
        trigger DirectoryChanged
          entry String

        module Billing

          feature Invoices

            slice Automation Sync

              specification SyncingOnAChange
                given clock "2026-10-05T08:00:00Z"
                given capture LegacyInvoices
                  id = "1"
                  status = "sent"
                when trigger DirectoryChanged
                  entry = "ada"
                then events in any order

              specification ChasingEveryMorning
                when clock "2026-10-06T08:00:00+02:00"
                then error

              specification SeeingAPayment
                when capture LegacyInvoices
                  id = "1"
                  status = "paid"
                then error

              specification ListingInvoices
                when query AllInvoices
                  status = "sent"
                then result exactly
                  number = "INV-1"
                then result
                  number = "INV-2"

              specification FindingNothing
                when query AllInvoices
                  status = "paid"
                then no result

        """;

    string _printed;

    void Because() => _printed = _printer.Print(_compiler.Parse(Source).Value!);

    [Fact] void should_print_the_document_as_written() => _printed.ShouldEqual(Source);
}
