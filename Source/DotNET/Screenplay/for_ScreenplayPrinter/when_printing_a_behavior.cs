// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_behavior : given.a_printer
{
    // The query the continuation refreshes is declared, so the only thing under test is the behavior itself -
    // an operand that resolves produces no diagnostic, which is half of what this asserts.
    const string Source =
        """
        behavior ConfirmThenExecute
          description "Asks first, then does it."
          parameter command
          parameter message String
          order 10

          on click
            where item.status == "Open"
            confirm message
              on success
                execute command
                  with invoiceId from item.invoiceId
                  on success
                    close dialog
                    refresh Invoices
                    notify info $strings.done
                  on failure
                    notify error $strings.failed

        module Invoicing
          feature InvoiceManagement
            slice StateView InvoiceList
              readmodel Invoice
                invoiceId Uuid

              query Invoices => Invoice[]
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_behavior_header() => _roundtrip.Printed.ShouldContain("behavior ConfirmThenExecute");
    [Fact] void should_print_the_description() => _roundtrip.Printed.ShouldContain("description \"Asks first, then does it.\"");
    [Fact] void should_print_the_untyped_parameter() => _roundtrip.Printed.ShouldContain("parameter command");
    [Fact] void should_print_the_typed_parameter() => _roundtrip.Printed.ShouldContain("parameter message String");
    [Fact] void should_print_the_order() => _roundtrip.Printed.ShouldContain("order 10");
    [Fact] void should_print_the_trigger() => _roundtrip.Printed.ShouldContain("on click");
    [Fact] void should_print_the_condition() => _roundtrip.Printed.ShouldContain("where item.status == \"Open\"");
    [Fact] void should_print_the_argument() => _roundtrip.Printed.ShouldContain("with invoiceId from item.invoiceId");
    [Fact] void should_print_the_nested_continuations() => _roundtrip.Printed.ShouldContain("on success");
    [Fact] void should_print_the_failure_continuation() => _roundtrip.Printed.ShouldContain("on failure");
    [Fact] void should_print_the_localized_notification() => _roundtrip.Printed.ShouldContain("notify info $strings.done");
    [Fact] void should_print_the_message_parameter_unquoted() => _roundtrip.Printed.ShouldContain("confirm message");
    [Fact] void should_not_quote_the_message_parameter() => _roundtrip.Printed.ShouldNotContain("confirm \"message\"");

    [Fact] void should_preserve_the_name() => Behavior.Name.ShouldEqual("ConfirmThenExecute");
    [Fact] void should_preserve_the_order() => Behavior.Order.ShouldEqual(10);
    [Fact] void should_preserve_the_parameters() => Behavior.Parameters.Select(parameter => parameter.Name).ShouldContainOnly("command", "message");
    [Fact] void should_preserve_one_binding() => Behavior.Bindings.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_trigger_kind() => ((BuiltInInteractionTriggerSyntax)Binding.Trigger).Kind.ShouldEqual(InteractionTriggerKind.Click);
    [Fact] void should_preserve_the_condition() => Binding.Condition.ShouldEqual("item.status == \"Open\"");
    [Fact] void should_preserve_the_confirm_as_the_only_top_level_action() => Binding.Actions.Single().ShouldBeOfExactType<ConfirmActionSyntax>();
    [Fact] void should_nest_the_execute_under_the_confirms_success() => Confirm.OnSuccess.Single().ShouldBeOfExactType<ExecuteCommandActionSyntax>();
    [Fact] void should_preserve_the_executes_argument() => Execute.Arguments.Single().Binding.ShouldEqual("item.invoiceId");
    [Fact] void should_preserve_the_three_actions_on_success() => Execute.OnSuccess.Count().ShouldEqual(3);
    [Fact] void should_preserve_the_failure_branch() => Execute.OnFailure.Single().ShouldBeOfExactType<NotifyActionSyntax>();

    BehaviorSyntax Behavior => _roundtrip.Reparsed.Value!.Behaviors.Single();
    InteractionBindingSyntax Binding => Behavior.Bindings.Single();
    ConfirmActionSyntax Confirm => (ConfirmActionSyntax)Binding.Actions.Single();
    ExecuteCommandActionSyntax Execute => (ExecuteCommandActionSyntax)Confirm.OnSuccess.Single();
}
