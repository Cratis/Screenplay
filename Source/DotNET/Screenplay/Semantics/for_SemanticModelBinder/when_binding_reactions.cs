// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_reactions : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        concept InvoiceId : Uuid
        trigger PaymentFileArrived
          invoiceId InvoiceId
          amount Decimal
        module Billing
          feature Invoices
            slice StateChange CloseInvoice
              command CloseInvoice
                invoiceId InvoiceId identifier
                reason String
                produces InvoiceClosed
                  for invoiceId
                  reason = reason
              event InvoiceClosed
                reason String
            slice Automation Payments
              reaction Importer
                when PaymentFileArrived
                  invoiceId
                  amount
                  produces PaymentImported
                    for invoiceId
                    amount = amount
                    importedAt = $context.occurred
                when PaymentImported
                  amount
                  invokes CloseInvoice
                    invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                    reason = "paid"
                where amount > 0
              reaction Scheduled
                when Shutdown
                every 15 minutes
                  produces Swept
                    for "sweeps"
                at 07:30 on Monday
                at 06:00 on day 1
              event PaymentImported
                amount Decimal
                importedAt DateTime
              event Swept
                note String?
        """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v6() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_declare_the_application_trigger() => _result.Value!.Model.Application.Triggers.Single().Properties.Select(_ => _.Name).ShouldContainOnly("invoiceId", "amount");
    [Fact] void should_keep_the_triggers_in_authored_order() => Triggers.Select(_ => _.Kind).ShouldContainOnly(SemanticReactionTriggerKind.ApplicationTrigger, SemanticReactionTriggerKind.Event, SemanticReactionTriggerKind.Shutdown, SemanticReactionTriggerKind.Interval, SemanticReactionTriggerKind.Schedule, SemanticReactionTriggerKind.Schedule);
    [Fact] void should_append_to_the_trigger_value_it_names() => Triggers[0].Produces.Single().DestinationType.ShouldEqual(SemanticTypeReference.ForConcept(_result.Value!.Model.Application.Concepts.Single().Id));
    [Fact] void should_read_values_from_the_application_trigger() => ((SemanticResolvedExpression)Triggers[0].Produces.Single().Mappings[0].Source).Root.ShouldEqual(SemanticExpressionRootKind.Trigger);
    [Fact] void should_narrow_the_triggers_that_carry_the_value() => Triggers[0].Where.ShouldNotBeNull();
    [Fact] void should_keep_the_separate_unguarded_clock_reaction() => Triggers[3].Where.ShouldBeNull();
    [Fact] void should_invoke_the_command() => Triggers[1].Invokes.Single().Mappings.Length.ShouldEqual(2);
    [Fact] void should_count_an_interval_in_seconds() => Triggers[3].Every.ShouldEqual(900L);
    [Fact] void should_append_a_clock_reaction_to_a_literal_event_source() => Triggers[3].Produces.Single().DestinationType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    [Fact] void should_schedule_a_weekday() => (Triggers[4].At, Triggers[4].OnDayOfWeek).ShouldEqual((27_000, (int?)1));
    [Fact] void should_schedule_a_day_of_the_month() => (Triggers[5].At, Triggers[5].OnDayOfMonth).ShouldEqual((21_600, (int?)1));

    SemanticReactionTrigger[] Triggers => [.. _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Name == "Payments").Reactions.SelectMany(reaction => reaction.Triggers)];
}
