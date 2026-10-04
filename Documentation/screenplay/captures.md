# Captures

Captures convert external data into events — change data capture as a first-class construct. The body of a `capture` block is written in the **Change Data Capture Language (CDL)** — an embedded sub-grammar the Screenplay parser delegates to (see [Sub-language Pluggability](sub-languages.md)). Captures live inside `Translate` slices.

## Syntax

```screenplay
capture <Name>
  <CDL grammar>
```

## Example

```screenplay
capture LegacyInvoiceCapture
  source api
    api LegacyInvoicingApi
    route /invoices
    poll 5m
  key id
  map
    status = status translate
      "utkast"     => draft
      "sendt"      => sent
      "betalt"     => paid
      "kansellert" => cancelled
    split contactName by ","
      firstName
      lastName
  append InvoiceStatusChanged
    when status
      invoiceId = $.id
      status    = $.status
      changedAt = $context.occurred
  append InvoicePaidFromSent
    when status from "sent" to "paid"
      invoiceId = $.id
      paidAt    = $context.occurred
  children lineItems identified by lineNumber
    map
      productName = name
    append InvoiceLineItemAdded
      when added
        invoiceId  = $.id
        lineNumber = $.lineNumber
        quantity   = $.quantity
        unitPrice  = $.unitPrice
    append InvoiceLineItemRemoved
      when removed
        invoiceId  = $.id
        lineNumber = $.lineNumber
  nested billingContact
    map
      contactName = name
    append BillingContactUpdated
      when email
        invoiceId = $.id
        email     = $.email
```

## The CDL vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `source api` / `source webhook` / `source message` | Where the captured data comes from, with source-specific settings (`api`/`route`/`poll` for `api`, `path` for `webhook`, `topic` for `message`). |
| `key <property>` | Which source property identifies an instance. |
| `map` | Maps, splits and translates source values before events are appended. |
| `<property> = <source>` | Renames a source value onto a property, or assigns a `` `template` `` built from source values. |
| `translate` | Maps source values to Screenplay values (`"utkast" => draft`). |
| `split <source> by "<separator>"` | Splits one source value into several target properties. |
| `append <EventType>` | Appends an event when the source changes, guarded by a `when` trigger. |
| `tag <value>` | Attaches a [tag](events.md#tags) to the appended event — a static identifier or string, or a `$context.` expression. |
| `when <property>` | Appends when the named property changes. |
| `when <property> or <property>` / `when <property> and <property>` | Appends when any (`or`) or all (`and`) of the named properties change. |
| `when <property> from <value> to <value>` | Appends when the named property transitions between two specific values. |
| `` when `<expression>` `` | Appends when a raw template-literal expression evaluates to true. |
| `when added` / `when removed` | Appends when an item appears in / disappears from the source. |
| `children <collection> identified by <key>` | Captures changes in a child collection, with its own optional `map`. |
| `nested <property>` | Captures changes to a single nullable child object, with its own optional `map`. |
| `$.<property>` | A value from the current source item. |
| `$context.occurred` | The capture timestamp. |

## In the executable semantic model

Captures bind to the executable semantic model as ESM v6 ([decision 0022](https://github.com/Cratis/Screenplay/blob/main/decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)).
The reference evaluator is handed records - `given capture` and `when capture` in a [specification](specifications.md#clocks-triggers-and-captures) -
and never contacts a `source`, which stays realization metadata. For each record:

- `map` operations apply in order, each to the record as the ones before it left it. A field `map` does not
  touch passes through unchanged.
- A condition compares the mapped record with the mapped record the capture last saw for the same `key`. A
  field the earlier record lacked has changed when the new one has it, so a first record has changed in every
  field it carries. `when status from "sent" to "paid"` compares mapped values.
- `` when `<expression>` `` holds a small portable grammar: fields, string, number, boolean and `null`
  literals, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!` and parentheses. Ordering compares numbers only.
- `$.<field>` reads the item being evaluated - the record, a child or the nested record - and falls back to the
  enclosing record when the item lacks the field. `$context.occurred` is the scenario's `given clock`.
- Everything is appended to the event source the record's `key` names. Its type is the one the event's
  commands give it, and text when no command produces the event.
- The record's own appends run first, then each `children` collection's - current children in order, then the
  removed ones - then each `nested` record's.

A capture without a `key`, or a condition outside the grammar above, does not bind (`PLAY0268`).

## Full CDL grammar

The authoritative CDL grammar - including the `split`, `nested` and richer `when` forms above - is published as the [Capture Declaration Language grammar](captures/grammar.md), mirroring how the [Projection Declaration Language grammar](projections/grammar.md) documents PDL.
