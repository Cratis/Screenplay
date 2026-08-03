// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reactor_without_a_body : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice Automation ReconcilePayments
              reactor PaymentReconciler
                description "Matches settled payments against outstanding invoices"
                on InvoicePaid
                on InvoiceMarkedOverdue
                  description "Re-checks whether a late payment has since arrived"

            slice StateChange ChangeInvoiceStatus
              event InvoicePaid
                paidAt DateTime

              event InvoiceMarkedOverdue
                overdueAt DateTime
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_reactor_description() => _roundtrip.Printed.ShouldContain("description \"Matches settled payments against outstanding invoices\"");
    [Fact] void should_print_the_trigger_description() => _roundtrip.Printed.ShouldContain("description \"Re-checks whether a late payment has since arrived\"");
    [Fact] void should_preserve_both_triggers() => Reactor.Triggers.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_bare_trigger() => Reactor.Triggers.First().ShouldNotBeNull();
    [Fact] void should_leave_the_bare_trigger_without_a_body() => (Reactor.Triggers.First() is { File: null, Code: null, Description: null }).ShouldBeTrue();

    ReactorSyntax Reactor => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.First().Reactors.Single();
}
