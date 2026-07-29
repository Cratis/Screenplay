// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reactor : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange MarkInvoiceOverdue
              command MarkInvoiceOverdue
                invoiceId Uuid

              event InvoiceRegistered
                invoiceId Uuid

              event CustomerNotified
                invoiceId Uuid

            slice Automation NotifyCustomer
              reactor CustomerNotifier
                on InvoiceRegistered
                  produces CustomerNotified
                  executes MarkInvoiceOverdue
                  file Reactors/CustomerNotifier.cs
                on CustomerNotified
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_produced_event() => _roundtrip.Printed.ShouldContain("produces CustomerNotified");
    [Fact] void should_print_the_executed_command() => _roundtrip.Printed.ShouldContain("executes MarkInvoiceOverdue");
    [Fact] void should_preserve_the_produced_event() => Trigger(_roundtrip.Reparsed, 0).Produces!.Single().Event.ShouldEqual("CustomerNotified");
    [Fact] void should_preserve_the_executed_command() => Trigger(_roundtrip.Reparsed, 0).Executes!.Single().Command.ShouldEqual("MarkInvoiceOverdue");
    [Fact] void should_preserve_the_file_reference() => Trigger(_roundtrip.Reparsed, 0).File!.Path.ShouldEqual("Reactors/CustomerNotifier.cs");
    [Fact] void should_preserve_the_trigger_without_a_body() => Trigger(_roundtrip.Reparsed, 1).Event.ShouldEqual("CustomerNotified");
    [Fact] void should_keep_the_second_trigger_bodyless() => Trigger(_roundtrip.Reparsed, 1).File.ShouldBeNull();

    static ReactorTriggerSyntax Trigger(CompilationResult<ApplicationSyntax> result, int index) =>
        result.Value!.Modules.Single().Features.Single().Slices
            .Single(_ => _.Name == "NotifyCustomer").Reactors.Single().Triggers.ElementAt(index);
}
