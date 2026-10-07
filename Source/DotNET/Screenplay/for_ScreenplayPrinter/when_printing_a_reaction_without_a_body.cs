// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reaction_without_a_body : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice Automation ReconcilePayments
              reaction PaymentReconciler
                description "Matches settled payments against outstanding invoices"
                when InvoicePaid
                when InvoiceMarkedOverdue
                  description "Re-checks whether a late payment has since arrived"

            slice StateChange ChangeInvoiceStatus
              event InvoicePaid
                paidAt DateTime

              event InvoiceMarkedOverdue
                overdueAt DateTime
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_report_both_original_backward_trigger_references() => _roundtrip.Original!.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual("PLAY0516@6", "PLAY0516@7");
    [Fact] void should_report_both_reprinted_backward_trigger_references() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldEqual("PLAY0516@9", "PLAY0516@10");
    [Fact] void should_report_only_information() => _roundtrip.Original!.Diagnostics.Concat(_roundtrip.Reparsed.Diagnostics).All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_reaction_description() => _roundtrip.Printed.ShouldContain("description \"Matches settled payments against outstanding invoices\"");
    [Fact] void should_print_the_trigger_description() => _roundtrip.Printed.ShouldContain("description \"Re-checks whether a late payment has since arrived\"");
    [Fact] void should_preserve_both_triggers() => Reaction.Triggers.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_bare_trigger() => Reaction.Triggers.First().ShouldNotBeNull();
    [Fact] void should_leave_the_bare_trigger_without_a_body() => (Reaction.Triggers.First() is { File: null, Code: null, Description: null }).ShouldBeTrue();

    ReactionSyntax Reaction => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.First().Reactions.Single();
}
