// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_invocation_expectations : given.a_printer
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice Automation Closing
              event InvoicePaid
                invoiceId Uuid
              command CloseInvoice
                invoiceId Uuid identifier
                reason String
              reaction Closer
                when InvoicePaid
                  invoiceId
                  invokes CloseInvoice
                    invoiceId = invoiceId
                    reason = "paid"
              specification S
                when append InvoicePaid
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then invokes CloseInvoice
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  reason = "paid"
                then invokes CloseInvoice
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_every_invocation_with_its_values() => _roundtrip.Printed.ShouldContain("        then invokes CloseInvoice\n          invoiceId = \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n          reason = \"paid\"\n        then invokes CloseInvoice\n");
    [Fact] void should_keep_the_syntax() => SyntaxJson.Serialize(_roundtrip.Reparsed.Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(_roundtrip.Original!.Value!).GetRawText());
}
