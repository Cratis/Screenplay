---
title: "Screenplay captures: turn external data into events"
description: "Screenplay captures turn external records or public events into local events. Choose a key, transform fields, and specify which changes matter."
---

# Captures

A Screenplay capture turns external records or another application's public events into local events when the data changes. Use one to translate an outside system's data into facts your application owns. Captures live inside `Translate` slices and use the Change Data Capture Language (CDL), an embedded grammar described in [Sub-language Pluggability](sub-languages.md).

Start with the identity. `key` names the source property that identifies one instance across observations. Its value also determines the event source to which the capture appends. An event source groups the events for one instance, such as an invoice. Then use `map` to transform the source values and `append` to say which local events record a meaningful change.

## Syntax

```screenplay
capture <Name>
  <CDL grammar>
```

## Example

Suppose a legacy invoicing API reports statuses in Norwegian and your application records them as `draft`, `sent`, `paid` and `cancelled`. This capture translates those values, compares each invoice with its earlier state, and records the changes. The API declaration describes the integration; the reference evaluator checks supplied records without contacting the API.

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

Consider an earlier record with `id` `"invoice-42"` and `status` `"sendt"`. When the next record for `"invoice-42"` has `status` `"betalt"`, the map turns the earlier status into `sent` and the new status into `paid`. Both `when status` and `when status from "sent" to "paid"` hold, so the capture appends `InvoiceStatusChanged` followed by `InvoicePaidFromSent` to event source `"invoice-42"`. Both events receive that invoice id; `InvoiceStatusChanged` also receives the mapped status `paid`. `$context.occurred` supplies `changedAt` and `paidAt`.

A first record already marked `"betalt"` does not trigger either status append: there is no earlier status to compare. Add a separate `when added` append if you also need an initial-import event. In a specification for this API capture, use `given capture` for the earlier record, `when capture` for the new one, and `given clock` for the timestamp. The [specifications guide](specifications.md#clocks-triggers-and-captures) shows those fixtures.

The same root `id` also owns the child and nested events. `children lineItems identified by lineNumber` matches each line against its earlier value, independently of list position; a new line appends `InvoiceLineItemAdded` and a removed line appends `InvoiceLineItemRemoved`. Within those mappings, `$.lineNumber` reads the line and `$.id` falls back to the enclosing invoice. `nested billingContact` compares one contact object and appends `BillingContactUpdated` when its email changes.

## Capturing from public events

When another application supplies public events instead of API records, use `source events` in an inbound `Translate` slice. The capture reads each incoming event's content and appends private events owned by your application. In this example, the first shipment observation for an order records `OrderShipped`:

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

The specification supplies `ShipmentDispatched` with `orderId` `"order-42"`. Because this is the first observation for that key, `when added` appends `OrderShipped` with event-source identity `"order-42"`. The payload's `orderId` selects the local identity; an incoming event's own routing identity can be different. To preserve a concept such as `OrderId` instead of a String identity, continue to [Typed event capture keys](captures/typed-event-keys.md).

One capture reads one origin: all its `from` events must have the exact same origin string (`PLAY0687`). Split different origins into separate captures. Every `from` event must be a public event with an origin, every `append` target must be a private event of this application, and `source events` belongs only to an explicitly inbound slice (`PLAY0610`, `PLAY0612`, `PLAY0616`, `PLAY0619`, `PLAY0620`).

Present earlier observations through `given <Event>` and the new observation through `when append <Event>`; their fields are typed by the declared event. `given capture` and `when capture` over events are compile errors (`PLAY0701`), with a repair only when there is exactly one `from` event. Unless the key is marked, a fixture's optional `for` identifies the foreign event source as opaque, non-empty text, not the capture's key. Use [Keying by the event source](#keying-by-the-event-source) when those identities must match. Rename follows a `from` line; the [diagnostics reference](diagnostics.md) lists the validation rules.

The key must be a required property of every `from` event (`PLAY0700`), never a context key. Each incoming event overlays its own top-level properties onto remembered state for that key; absent properties retain earlier values, while objects and lists replace whole values. Givens fold in authored order without appending. Keys come from the incoming event, not remembered state: empty or whitespace-only text fails execution with a contract rejection naming the capture, event and key, including before the action when a given has an empty key. Non-text, non-numeric keys are Unsupported; numeric and text keys stay distinct.

The first event is `added`, not a field modification. An unchanged overlay appends nothing. Field, `and`/`or` and transition guards hold only on modifications; `when added` holds only on the first event, and root `when removed` never holds. An append without `when` holds on any change; expression guards evaluate the after state only on a change. Maps apply to both states, and `$.field` reads the mapped merged state. `$context.occurred` is the incoming occurrence, supplied by the scenario's `given clock`; other context members are not admitted.

Delivery follows the fact's settled reactions, in capture semantic-identity order. Each capture append settles its own cascade before the next; the 1,000-fact scenario budget includes those appends. `children` and `nested` over events are executed: see [Lists and objects in public events](#lists-and-objects-in-public-events). Composite and list values used as scalars or watched by guards remain Unsupported. The reference stores no new capture state across specifications and does not prove redelivery deduplication, observer ordering, retry safety or context propagation. Stage's Cratis backend does not lower capture maps, expression guards, children or nested scopes. Its admitted record-level appends can be unguarded or use added, field-change and transition conditions.

### Keying by the event source

Use `key <property> is event source` when the foreign event's event-source identity must equal the payload key. Without the marker, the payload key still selects the capture's remembered state and local event source, but does not assert equality with the incoming identity. Only a `source events` capture with a single property key can declare the marker (`PLAY0809`; the diagnostic has no repair).

```screenplay
slice Translate ReceiveDispatches
  direction inbound
  event ParcelDispatched from "carrier"
    orderId String
  event DispatchRecorded
    orderId String
  capture DispatchFeed
    source events
      from ParcelDispatched
    key orderId is event source
    append DispatchRecorded
      orderId = $.orderId
```

When a fixture states a `for`, the reference compares it with the key: a `for` that differs from the key's value fails the run with a contract rejection naming the capture, the event and both values. A fixture without `for` takes the key as its source before projections and reactions see it, so `$eventSourceId` is the key. The check runs when the world is established, with or without an action. The marker does not change how records merge or which appends run. Its version and serialization requirements are described under [In the executable semantic model](#in-the-executable-semantic-model). Stage admits v33 before rendering, and may narrow the inbox read by the key.

### Lists and objects in public events

When a public event contains order lines, decide whether your application needs the whole list in one event or one event per line. The reference evaluator executes both forms below:

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
| `key <property> is event source` | On `source events`, promises every captured event is sourced by its key (ESM v33, `PLAY0809`). |
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

The Executable Semantic Model (ESM) is the compiler's portable representation of model meaning. Record captures bind as ESM v6 ([decision 0022](https://github.com/Cratis/Screenplay/blob/main/decisions/0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)). Captures over public events select ESM v9, or v32 when a [typed event key](captures/typed-event-keys.md) changes the captured identity. Adding `is event source` selects v33 and serializes `keyIsEventSource` beside the capture's `sourceEvents`; omitting the marker preserves the earlier canonical bytes. Lists and objects in captured events require no new language version.

For non-event sources, the reference evaluator is handed records through `given capture` and `when capture` in a [specification](specifications.md#clocks-triggers-and-captures). It never contacts a `source`; that declaration remains metadata for the consumer implementing the integration. For each record:

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
adding a compiler diagnostic. Stage's Cratis backend refuses capture maps, expression guards, children and nested scopes rather than approximating their behavior.

A capture without a `key`, or a condition outside the grammar above, does not bind (`PLAY0268`).

## Full CDL grammar

Use the [Capture Declaration Language grammar](captures/grammar.md) to check exact `split`, `nested` and `when` syntax. For the corresponding grammar used to build views from events, continue to the [Projection Declaration Language grammar](projections/grammar.md).
