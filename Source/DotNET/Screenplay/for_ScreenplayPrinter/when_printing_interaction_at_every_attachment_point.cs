// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// A behavior attaches at every level of the containment tree, and means the same thing at each. Attachments
/// are additive - a module's behavior and a screen's both run - so what this pins is that each level actually
/// carries, resolves and round-trips its own.
/// </summary>
public class when_printing_interaction_at_every_attachment_point : given.a_printer
{
    const string Source =
        """
        behavior ConfirmDestructive
          order 10

          on click
            confirm $strings.areYouSure

        layout AppShell
          main

          on enter
            refresh Invoices

        module Invoicing
          uses ConfirmDestructive

          screen template Workspace
            fits slot main
            content

            on enter
              refresh Invoices

          dialog template InvoiceDetails
            content

            on unload
              refresh Invoices

          form RegisterInvoiceForm for RegisterInvoice
            field invoiceId
            on submit navigate to InvoiceList
            on change
              refresh Invoices

          feature InvoiceManagement
            uses ConfirmDestructive

            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

            slice StateView InvoiceList
              readmodel Invoice
                invoiceId Uuid

              query Invoices => Invoice[]

              screen InvoiceList
                on enter
                  refresh Invoices
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_attach_to_the_layout() => Application.Layouts!.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_attach_to_the_module() => Module.UsedBehaviors.Single().Behavior.ShouldEqual("ConfirmDestructive");
    [Fact] void should_attach_to_the_screen_template() => Module.ScreenTemplates.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_attach_to_the_dialog_template() => Module.DialogTemplates!.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_attach_to_the_form() => Module.Forms!.Single().Behaviors.Count().ShouldEqual(1);
    [Fact] void should_attach_to_the_feature() => Module.Features.Single().UsedBehaviors.Count().ShouldEqual(1);
    [Fact] void should_attach_to_the_screen() => Screen.Directives.OfType<ScreenBehaviorSyntax>().Count().ShouldEqual(1);

    // 'on submit navigate to <Screen>' predates the interaction model and keeps its one line meaning; any
    // other 'on' in a form body is a behavior. Both in one form, so the two cannot quietly swallow each other.
    [Fact] void should_keep_the_existing_submit_navigation() => Module.Forms!.Single().OnSubmit!.Screen.ShouldEqual("InvoiceList");
    [Fact] void should_print_the_submit_navigation_on_one_line() => _roundtrip.Printed.ShouldContain("on submit navigate to InvoiceList");
    [Fact] void should_read_the_other_form_trigger_as_a_behavior() => ((BuiltInInteractionTriggerSyntax)Module.Forms!.Single().Behaviors.Single().Bindings.Single().Trigger).Kind.ShouldEqual(InteractionTriggerKind.Change);

    ApplicationSyntax Application => _roundtrip.Reparsed.Value!;
    ModuleSyntax Module => Application.Modules.Single();
    ScreenSyntax Screen => Module.Features.Single().Slices.SelectMany(slice => slice.Screens).Single();
}
