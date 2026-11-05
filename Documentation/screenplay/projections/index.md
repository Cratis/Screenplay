---
title: "Screenplay projections: build read models from events"
description: "Build a keyed read model from events with Screenplay projections. Choose which instance each event updates, map its data, and include event metadata."
---

<a id="projections"></a>

A Screenplay projection turns events into a read model: the stored view that a query can return to a screen or a command can consult. Use one when you need to describe how recorded facts create or change that view, including which instance each event updates.

Start with the target read model, then add a `from` block for each event that changes it. Each block selects an instance by key and maps values onto it. Without an explicit key, an event selects the instance identified by its event source ID. A `key` on the projection itself does not supply a default; put it on each relevant `from` or its individual events.

The indented body uses the Projection Declaration Language (PDL), a grammar embedded in Screenplay (see [Sub-language Pluggability](../sub-languages.md)). Cratis Chronicle also accepts this language on its own; [Chronicle projections](/chronicle/projections/projection-declaration-language/) explains how it hosts and runs it. Screenplay's [Semantic Model](semantic-model.md) explains which forms bind to executable behavior and which the reference evaluator can run.

## Syntax

```screenplay
projection <Name> => <ReadModel>
  <PDL grammar>
```

## Example

This invoice view uses `invoiceId` from each root event to select the invoice to update. Registration supplies its initial details; later events change its status. The join brings in a customer name, and the child collection gives each line item its own identity within the invoice.

```screenplay
projection InvoiceDetails => InvoiceDetailsReadModel
  from InvoiceRegistered key invoiceId
    customerId    = customerId
    invoiceNumber = invoiceNumber
    currency      = currency
    status        = "draft"
    registeredAt  = $eventContext.occurred
  from InvoiceSent key invoiceId
    status = "sent"
    sentAt = sentAt
  from InvoicePaid key invoiceId
    status = "paid"
    paidAt = paidAt
  join customer on customerId
    with CustomerRegistered
      customerName = name
  children lineItems identified by lineNumber
    from InvoiceLineItemAdded key lineNumber
      parent invoiceId
      quantity  = quantity
      unitPrice = unitPrice
    remove with InvoiceLineItemRemoved key lineNumber
      parent invoiceId
```

Explicit assignments handle renamed values, constants and metadata such as `$eventContext.occurred`. AutoMap also copies compatible, same-named event properties unless disabled; explicit mappings take precedence.

To build your first view, follow [From Event](from-event.md), choose its [Keys](keys.md), then fill in [Property Mapping](property-mapping.md). For timestamped examples in executable specifications, [Event Context](event-context.md#in-the-semantic-model) explains the required clock and the reference evaluator's limits.

## The PDL vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `from <EventType> key <expression>` | Which value identifies the read model instance for this event. |
| `from <EventType>` | Maps event properties onto the read model. Same-named properties map automatically. |
| `join <property> on <key>` | Joins related state by a key property. |
| `children <collection> identified by <key>` | Projects events into a child collection. |
| `parent <property>` | Identifies the owning parent of a child. |
| `remove with <EventType>` | Removes the instance (or child) when the event occurs. |
| `increment` / `decrement <property>` | Counters maintained per event. |
| `clear <property>` | Removes the value the property holds. |
| `$eventContext.occurred` | The timestamp of the event being projected. |
| `variant <Name>` / `enters on <EventType>` | One of several mutually exclusive named read models sharing the projection's identity. |

Counter operations update the instance selected by that event's key:

```screenplay
projection InvoiceSummary => InvoiceSummaryReadModel
  from InvoiceRegistered
    key "global"
    increment totalCount
    increment draftCount
  from InvoiceSent
    decrement draftCount
    increment sentCount
  from InvoiceCancelled
    decrement totalCount
```

Here only `InvoiceRegistered` selects `"global"`; the other handlers use their event source IDs. To accumulate all three event types into one summary, give each `from` the same `key "global"`. A constant key is not inherited by later handlers.

## Several projections in one slice

A slice is one behavior, and a behavior often needs more than one read model. The list a screen binds to is shaped for a reader; the state a command consults before it decides is shaped for a decision. Both are built from the same events by the same slice, so both `projection` blocks live in it:

```screenplay
slice StateView CustomerPortalReport
  query GetPortalReport => PortalReportReadModel
    by customerId CustomerId

  projection PortalReport => PortalReportReadModel
    from PortalInvitationSent
      invitedAt = $eventContext.occurred

  projection RevokedPortalToken => RevokedPortalTokenReadModel
    from PortalTokenRevoked
      revokedAt = $eventContext.occurred
```

Declare as many as the behavior needs. Each names its own read model, and [printing](../printing.md) writes them back out in declaration order. Keep views of the same behavior together rather than splitting them solely because their data shapes differ.

A projection header can also carry one quoted or fenced-text `description` to record authoring intent. It is report-only (`PLAY0270`), not executable behavior, and printing places it before `file` and builder directives. It does not count as a projection directive. Variants and nested blocks do not accept it, and Markdown `documentation` is not supported here.

## Projecting to an event

In an explicitly outbound `Translate` slice, a `projection` (or `reducer`) may name the slice's public event after `=>` instead of a read model. It builds the public event from private events:

```screenplay
slice Translate PublishOrderShipped
  direction outbound
  public event OrderShipped
    orderId String
    status String
  projection OrderShippedPublisher => OrderShipped
    from OrderPacked
      status = "packed"
    from ShipmentDispatched
      status = "shipped"
```

The target is an event when `=>` names a declared event and no `readmodel` of that name. Its `from` and `on` events must be private local events (`PLAY0611`), the target must be the slice's one local public event (`PLAY0609`, `PLAY0613`, `PLAY0617`), and the form is an error anywhere but an explicitly outbound translation (`PLAY0618`). It selects ESM v9. The reference evaluator folds the private events deterministically and publishes one public instance per target key. A published instance keeps the event source and context of the fact that produced it, so specifications type its `for` from those facts. A reducer that targets an event has an opaque transition body, so the reference evaluator reports Unsupported for specifications that depend on it. Renaming the event refuses while a `=>` target names it.

## Topics

- [From Event](from-event.md) - Define rules that trigger when events occur
- [Property Mapping](property-mapping.md) - Map event data to read model properties
- [Auto-Map](from-event.md#with-automap) - Automatically map matching properties
- [Keys](keys.md) - Explicit and composite keys for projection instances
- [Event Context](event-context.md) - Access event metadata like timestamps and correlation IDs
- [From Every](from-every.md) - Apply rules to all events the projection subscribes to
- [From All](from-all.md) - Subscribe to all event types without filtering
- [Counters](from-event.md#counter-operations) - Increment, decrement, and count operations
- [Arithmetic](arithmetic.md) - Add and subtract operations
- [Joins](joins.md) - Combine data from related events
- [Children](grammar.md#children-block) - Define nested collections
- [Nested Objects](grammar.md#nested-block) - Single nullable child objects
- [Removal](removal.md) - Remove projection instances based on events
- [Variants](variants.md) - Mutually exclusive named read models sharing one projection identity
- [Graph projections](graphs.md) - Project events onto a property graph of nodes and edges
- [Expressions](grammar.md#expressions) - Understanding expression syntax
- [Semantic Model](semantic-model.md) - How projections bind to the executable semantic model, with Chronicle as the semantic reference
- [Grammar (EBNF)](grammar.md) - Complete formal grammar specification
