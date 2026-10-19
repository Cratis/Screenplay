---
title: "Captures"
description: "Capture blocks turn external data into events with the Change Data Capture Language, as a first-class Screenplay construct."
---

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

## Capturing from public events

An inbound `Translate` slice turns the public events of another application into private events of its own. The capture reads them with `source events`, and the captured item is the incoming event's content:

```screenplay
slice Translate TrackShipments
  direction inbound
  event ShipmentDispatched from "shipping"
    orderId String
    status String
  event ShipmentDelivered from "shipping"
    orderId String
    status String
  event OrderShipped
    orderId String
  capture ShipmentTracking
    source events
      from ShipmentDispatched
      from ShipmentDelivered
    key orderId
    append OrderShipped
      when added
        orderId = $.orderId
  specification RecordingTheFirstDispatch
    when append ShipmentDispatched
      orderId = "order-42"
      status = "dispatched"
    then OrderShipped
      for "order-42"
      orderId = "order-42"
```

One capture reads one origin: all its `from` events must have the exact same origin string (`PLAY0687`); split events from different origins into separate captures. Every `from` event must be a public event with an origin, an `append` event must be a private event of this application, and `source events` belongs only to an explicitly inbound slice (`PLAY0610`, `PLAY0612`, `PLAY0616`, `PLAY0619`, `PLAY0620`). The form selects ESM v9 and binds to an executable capture over events. Present the declared event through `given <Event>` or `when append <Event>`; its fields are typed by that event. `given capture` and `when capture` over events are compile errors (`PLAY0701`), with a repair only when there is exactly one `from` event. A fixture's optional `for` identifies the foreign event source as opaque, non-empty text, not the capture's key. Rename follows a `from` line; see the [diagnostics](diagnostics.md) for the full list.

The key must be a required property of every `from` event (`PLAY0700`), never a context key. Each incoming event overlays its own top-level properties onto remembered state for that key; absent properties retain earlier values, while objects and lists replace whole values. Givens fold in authored order without appending. Keys come from the incoming event, not remembered state: empty or whitespace-only text fails execution with a contract rejection naming the capture, event and key, including before the action when a given has an empty key. Non-text, non-numeric keys are Unsupported; numeric and text keys stay distinct.

The first event is `added`, not a field modification. An unchanged overlay appends nothing. Field, `and`/`or` and transition guards hold only on modifications; `when added` holds only on the first event, and root `when removed` never holds. An append without `when` holds on any change; expression guards evaluate the after state only on a change. Maps apply to both states, and `$.field` reads the mapped merged state. `$context.occurred` is the incoming occurrence, supplied by the scenario's `given clock`; other context members are not admitted.

Delivery follows the fact's settled reactions, in capture semantic-identity order. Each capture append settles its own cascade before the next; the 1,000-fact scenario budget includes those appends. `children` and `nested` over events are executed: see [Lists and objects in public events](#lists-and-objects-in-public-events). Composite and list values used as scalars or watched by guards remain Unsupported. The reference stores no new capture state across specifications and does not prove redelivery deduplication, observer ordering, retry safety or context propagation. Stage must still reject event-capture forms Chronicle cannot realize, including maps, expression guards and an append without `when`.

### Lists and objects in public events

A public event may carry a list of objects or an object, and a capture over events can carry it across the boundary two ways (executed by the reference evaluator, no new language version):

```screenplay
type StockLine
  sku String
  quantity Int

module Warehouse
  feature Reservations
    slice Translate ReceiveOrder
      direction inbound
      event OrderConfirmed from "sales"
        orderId String
        lines StockLine[]
      event ReservationRequested
        orderId String
        lines StockLine[]
      event LineReserved
        orderId String
        sku String
        quantity Int
      capture WholeOrder
        source events
          from OrderConfirmed
        key orderId
        append ReservationRequested
          when added
            orderId = $.orderId
            lines = $.lines
      capture PerLine
        source events
          from OrderConfirmed
        key orderId
        children lines identified by sku
          append LineReserved
            when added
              orderId = $.orderId
              sku = $.sku
              quantity = $.quantity
```

`lines = $.lines` copies the whole list into a list property of the same shape (matched by property name, so the private type may be a different type with the same members). `children lines identified by sku` makes one scope per element: an element whose `sku` has not been seen for this key is `added` and appends one private event, and `nested <object>` scopes into an object the same way. Children and nested scopes have the semantics of record captures. Another delivery with the same lines appends nothing; a delivery with an extra line appends only that line. A list that is empty or a nested object that is absent has no scopes. The element key must be present and unique in the list, or the capture is refused as it is for a record capture.

## The CDL vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `source events` | Reads the public events of another application instead of an external system; each `from <Event>` line names one consumed event. |
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
For non-event sources, the reference evaluator is handed records - `given capture` and `when capture` in a [specification](specifications.md#clocks-triggers-and-captures) -
and never contacts a `source`, which stays realization metadata. For each record:

- `map` operations apply in order, each to the record as the ones before it left it. A field `map` does not
  touch passes through unchanged.
- A condition compares the mapped record with the mapped record the capture last saw for the same typed `key`.
  With no previous state, the record is `added` only: `when added` holds, but field-change guards, `and`/`or`
  combinations and transitions do not. With previous state, a field the earlier record lacked has changed when
  the new one has it. `when status from "sent" to "paid"` compares mapped values.
- The first-observation rule applies independently to children and nested records. A child identity absent from
  the parent's previous collection, or a nested record whose field was missing or null, is `added` only.
  Every child of a first root record is new, even when other roots have children with the same identity.
- `` when `<expression>` `` holds a small portable grammar: fields, string, number, boolean and `null`
  literals, `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!` and parentheses. A field on its own is true when it
  holds the boolean `true`. Ordering compares numbers only. The C# binder parses the guard and reports `PLAY0268`
  for anything outside this set. The TypeScript compiler keeps the guard as text and checks only that its closing
  backtick is present; it does not read the operators. Outside inline code (embedded C# and screen code), this is the only place `&&`, `||` and `!` are admitted; see
  [Notation conventions](notation.md#capture-guards).
  Expressions evaluate the mapped current record even on its first observation; unguarded appends also run
  on a first record. Maps, expressions and unguarded appends keep their existing meaning.
- `$.<field>` reads the item being evaluated - the record, a child or the nested record - and falls back to the
  enclosing record when the item lacks the field. `$context.occurred` is the scenario's `given clock`.
- Everything is appended to the event source the record's `key` names. Its type is the one the event's
  commands give it, and text when no command produces the event. The key must match that type: for a text
  event source, write `PlantId = "1201"`, not `PlantId = 1201`. The reference runner rejects a numeric key
  without converting it to text and includes the key, value and required type in the specification failure.
- The record's own appends run first, then each `children` collection's - current children in order, then the
  removed ones in their earlier order - then each `nested` record's.

The reference checks record shapes, event-source keys and child identities (including duplicates) before
appending anything. After those preconditions pass, it evaluates each reached append in order, enforces its
constraints, projects it and settles its reactions before evaluating the next capture effect. The first
rejection or Unsupported stops the scenario. Facts and projections from earlier accepted appends remain;
the failed append adds neither. A later unsupported mapping or guard cannot mask an earlier constraint
rejection. This is not a capture-wide transaction; invoked commands keep their own atomicity.

`given capture` supplies immutable last-seen input for one specification. Running the specification does not
store a new last-seen record or acknowledge an external source, on success or failure. Supply the baseline
explicitly in each subsequent specification. Retained prefix facts do not imply successful source processing
or acknowledgement. A present object or collection used as a scalar mapping is Unsupported, not a missing
value or a passing `then error`.

Record captures and captures over events share Chronicle's added-only first-observation rule
([decision 0080](https://github.com/Cratis/Screenplay/blob/main/decisions/0080-treat-the-first-record-of-a-key-in-a-record-capture-as-added-only.md)).
Earlier reference execution treated every field of a first record as changed. If a specification relied on
that behavior, add a separate append with `when added` when the event should also be recorded on first sight,
and keep the field-guarded append for updates. If the scenario meant an update, seed the same key's previous
state with `given capture`. This corrects execution in ESM v6 and later without changing canonical bytes or
adding a compiler diagnostic. Stage must refuse or realize reference-only maps, expressions, children and
nested scopes that Chronicle does not support.

A capture without a `key`, or a condition outside the grammar above, does not bind (`PLAY0268`).

## Full CDL grammar

The authoritative CDL grammar - including the `split`, `nested` and richer `when` forms above - is published as the [Capture Declaration Language grammar](captures/grammar.md), mirroring how the [Projection Declaration Language grammar](projections/grammar.md) documents PDL.
