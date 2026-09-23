// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_screen_with_attached_interaction : given.a_printer
{
    // A complete little document rather than a fragment: the operands resolve, which is what proves the
    // interaction is made of model references and not of strings.
    const string Source =
        """
        behavior ConfirmThenExecute
          parameter command
          parameter message

          on click
            confirm message
              on success
                execute command

        module Invoicing
          dialog template InvoiceDetails
            content

          feature InvoiceManagement
            slice StateChange CancelInvoice
              command CancelInvoice
                invoiceId Uuid

            slice StateView InvoiceList
              readmodel Invoice
                invoiceId Uuid

              query Invoices => Invoice[]

              screen InvoiceList
                on enter
                  refresh Invoices

                uses ConfirmThenExecute
                  command CancelInvoice
                  message "$strings.confirmCancel"

                table Invoices
                  column invoiceId
                  on double click
                    open dialog InvoiceDetails
                      with invoiceId from item.invoiceId
                      on result
                        refresh Invoices
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_declare_exactly_one_behavior() => _roundtrip.Reparsed.Value!.Behaviors.Count().ShouldEqual(1);
    [Fact] void should_not_hoist_the_inline_binding_into_a_second_declaration() => Occurrences("behavior ").ShouldEqual(1);
    [Fact] void should_print_the_lifecycle_trigger() => _roundtrip.Printed.ShouldContain("on enter");
    [Fact] void should_print_the_attachment() => _roundtrip.Printed.ShouldContain("uses ConfirmThenExecute");
    [Fact] void should_print_the_attachment_arguments() => _roundtrip.Printed.ShouldContain("command CancelInvoice");
    [Fact] void should_print_the_double_click_trigger() => _roundtrip.Printed.ShouldContain("on double click");
    [Fact] void should_print_the_result_continuation() => _roundtrip.Printed.ShouldContain("on result");

    [Fact] void should_attach_an_anonymous_behavior_to_the_screen() => ScreenBehavior.Behavior.Name.ShouldBeNull();
    [Fact] void should_resolve_the_screen_trigger_as_enter() => ((BuiltInInteractionTriggerSyntax)ScreenBehavior.Behavior.Bindings.Single().Trigger).Kind.ShouldEqual(InteractionTriggerKind.Enter);
    [Fact] void should_attach_the_named_behavior_with_its_arguments() => Uses.Uses.Arguments.Count().ShouldEqual(2);
    [Fact] void should_attach_the_behavior_to_the_table() => Table.Behaviors.Single().Bindings.Single().Actions.Single().ShouldBeOfExactType<OpenDialogActionSyntax>();
    [Fact] void should_carry_the_dialog_result_continuation() => OpenDialog.OnResult.Single().ShouldBeOfExactType<RefreshQueryActionSyntax>();
    [Fact] void should_keep_the_column() => Table.Columns.Single().Property.ShouldEqual("invoiceId");

    int Occurrences(string text) => _roundtrip.Printed.Split(text).Length - 1;

    ScreenSyntax Screen => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Screens).Single();
    ScreenBehaviorSyntax ScreenBehavior => Screen.Directives.OfType<ScreenBehaviorSyntax>().Single();
    ScreenUsesBehaviorSyntax Uses => Screen.Directives.OfType<ScreenUsesBehaviorSyntax>().Single();
    ScreenTableSyntax Table => Screen.Directives.OfType<ScreenTableSyntax>().Single();
    OpenDialogActionSyntax OpenDialog => (OpenDialogActionSyntax)Table.Behaviors.Single().Bindings.Single().Actions.Single();
}
