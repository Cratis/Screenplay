// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_trigger_fires : given.a_v6_scenario
{
    const string Source =
        """
        concept InvoiceId : Uuid
        trigger PaymentFileArrived
          invoiceId InvoiceId
          amount Decimal
        module Billing
          feature Payments
            slice Automation Imports
              reaction Importer
                when PaymentFileArrived
                  invoiceId
                  amount
                  produces PaymentImported
                    for invoiceId
                    amount = amount
                when Startup
                  produces ImportsStarted
                    for "imports"
                    note = "started"
              event PaymentImported
                amount Decimal
              event ImportsStarted
                note String
              specification ImportingAPayment
                when trigger PaymentFileArrived
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  amount = 120
                then PaymentImported
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  amount = 120
              specification StartingUp
                when trigger Startup
                then ImportsStarted
                  for "imports"
                  note = "started"
        """;

    SemanticSpecificationRun _imported;
    SemanticSpecificationRun _started;

    void Establish() => Compile(Source);

    void Because()
    {
        _imported = Run("ImportingAPayment");
        _started = Run("StartingUp");
    }

    [Fact] void should_run_the_reaction_with_the_trigger_values() => _imported.Passed.ShouldBeTrue();
    [Fact] void should_fire_a_built_in_trigger() => _started.Passed.ShouldBeTrue();
}
