// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_capture_record_is_presented : given.a_v6_scenario
{
    const string Source =
        """
        module Billing
          feature Legacy
            slice Translate LegacySync
              capture LegacyInvoiceCapture
                source api
                  api LegacyInvoicingApi
                  route /invoices
                key id
                map
                  status = status translate
                    "sendt"  => sent
                    "betalt" => paid
                  split contactName by ","
                    lastName
                    firstName
                  summary = `${status} invoice`
                append LegacyStatusChanged
                  when status
                    status    = $.status
                    summary   = $.summary
                    changedAt = $context.occurred
                append LegacyPaidFromSent
                  when status from "sent" to "paid"
                    paidAt = $context.occurred
                append LegacyNeedsAttention
                  when status or contactName
                    source = "legacy"
                append LegacyFullyChanged
                  when status and contactName
                    source = "legacy"
                append LegacyFlagged
                  when `status == "paid" && overdue == true`
                    firstName = $.firstName
                children lineItems identified by lineNumber
                  map
                    productName = name
                  append LegacyLineAdded
                    when added
                      lineNumber  = $.lineNumber
                      productName = $.productName
                  append LegacyLineRemoved
                    when removed
                      lineNumber = $.lineNumber
                nested billingContact
                  append LegacyContactChanged
                    when email
                      email = $.email
              event LegacyStatusChanged
                status String
                summary String
                changedAt DateTime
              event LegacyPaidFromSent
                paidAt DateTime
              event LegacyNeedsAttention
                source String
              event LegacyFullyChanged
                source String
              event LegacyFlagged
                firstName String
              event LegacyLineAdded
                lineNumber Int
                productName String
              event LegacyLineRemoved
                lineNumber Int
              event LegacyContactChanged
                email String
              specification SeeingAnInvoicePaidInTheLegacySystem
                given clock "2026-10-02T12:00:00Z"
                given capture LegacyInvoiceCapture
                  id             = "inv-42"
                  status         = "sendt"
                  contactName    = "Lovelace, Ada"
                  overdue        = true
                  lineItems      = [{ "lineNumber": 1, "name": "Pens" }, { "lineNumber": 2, "name": "Paper" }]
                  billingContact = { "email": "ada@example.com" }
                when capture LegacyInvoiceCapture
                  id             = "inv-42"
                  status         = "betalt"
                  contactName    = "Lovelace, Ada"
                  overdue        = true
                  lineItems      = [{ "lineNumber": 1, "name": "Pens" }, { "lineNumber": 3, "name": "Ink" }]
                  billingContact = { "email": "ada@lovelace.org" }
                then LegacyStatusChanged
                  for "inv-42"
                  status    = "paid"
                  summary   = "paid invoice"
                  changedAt = "2026-10-02T12:00:00Z"
                then LegacyPaidFromSent
                  for "inv-42"
                  paidAt = "2026-10-02T12:00:00Z"
                then LegacyNeedsAttention
                  for "inv-42"
                  source = "legacy"
                then LegacyFlagged
                  for "inv-42"
                  firstName = "Ada"
                then LegacyLineAdded
                  for "inv-42"
                  lineNumber  = 3
                  productName = "Ink"
                then LegacyLineRemoved
                  for "inv-42"
                  lineNumber = 2
                then LegacyContactChanged
                  for "inv-42"
                  email = "ada@lovelace.org"
              specification SeeingAnInvoiceForTheFirstTime
                given clock "2026-10-02T12:00:00Z"
                when capture LegacyInvoiceCapture
                  id          = "inv-43"
                  status      = "sendt"
                  contactName = "Hopper, Grace"
                  overdue     = false
                then events in any order
                then LegacyStatusChanged
                  for "inv-43"
                  status    = "sent"
                  summary   = "sent invoice"
                  changedAt = "2026-10-02T12:00:00Z"
                then LegacyNeedsAttention
                  for "inv-43"
                  source = "legacy"
                then LegacyFullyChanged
                  for "inv-43"
                  source = "legacy"
        """;

    SemanticSpecificationRun _changed;
    SemanticSpecificationRun _first;

    void Establish() => Compile(Source);

    void Because()
    {
        _changed = Run("SeeingAnInvoicePaidInTheLegacySystem");
        _first = Run("SeeingAnInvoiceForTheFirstTime");
    }

    [Fact] void should_append_what_changed_since_the_record_it_last_saw() => _changed.Passed.ShouldBeTrue();
    [Fact] void should_treat_every_field_of_a_first_record_as_changed() => _first.Passed.ShouldBeTrue();
    [Fact] void should_append_to_the_event_source_the_key_names() => ((SemanticAccepted)_changed.Execution).Facts.All(fact => fact.Destination == SemanticValue.Text("inv-42")).ShouldBeTrue();
}
