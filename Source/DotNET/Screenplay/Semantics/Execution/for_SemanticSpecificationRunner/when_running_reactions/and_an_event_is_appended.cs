// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_an_event_is_appended : given.a_v6_scenario
{
    const string Source =
        """
        concept PaymentId : Uuid
        module Billing
          feature Payments
            slice StateChange SettlePayment
              command SettlePayment
                paymentId PaymentId identifier
                amount Decimal
                produces PaymentSettled
                  for paymentId
                  amount = amount
              event PaymentSettled
                amount Decimal
            slice Translate PaymentTranslation
              reaction Translator
                when PaymentSettled
                  amount
                  produces PaymentRecorded
                    amount = amount
                    method = "card"
                where amount > 0
              event PaymentRecorded
                amount Decimal
                method String
              specification RecordingASettledPayment
                when append PaymentSettled
                  for "4e5f6a7b-8c9d-4e0f-a1b2-c3d4e5f6a7b8"
                  amount = 120
                then PaymentRecorded
                  for "4e5f6a7b-8c9d-4e0f-a1b2-c3d4e5f6a7b8"
                  amount = 120
                  method = "card"
              specification IgnoringAnEmptySettlement
                when append PaymentSettled
                  for "4e5f6a7b-8c9d-4e0f-a1b2-c3d4e5f6a7b8"
                  amount = 0
                then PaymentRecorded
                  for "4e5f6a7b-8c9d-4e0f-a1b2-c3d4e5f6a7b8"
                  amount = 0
                  method = "card"
        """;

    SemanticSpecificationRun _recorded;
    SemanticSpecificationRun _ignored;

    void Establish() => Compile(Source);

    void Because()
    {
        _recorded = Run("RecordingASettledPayment");
        _ignored = Run("IgnoringAnEmptySettlement");
    }

    [Fact] void should_compare_what_followed_the_appended_event() => _recorded.Passed.ShouldBeTrue();
    [Fact] void should_append_to_the_event_source_of_the_event_that_set_it_off() => ((SemanticAccepted)_recorded.Execution).Facts[1].Context!.EventSource.Value.ShouldEqual(SemanticValue.Text("4e5f6a7b-8c9d-4e0f-a1b2-c3d4e5f6a7b8"));
    [Fact] void should_not_react_to_an_occurrence_its_where_excludes() => _ignored.Failures.ShouldContain("Expected 1 fact(s), got 0.");
}
