# Projections

Projections declare how events are projected into a queryable read model. The body of a `projection` block is written in the **Projection Declaration Language (PDL)** — an embedded sub-grammar. The Screenplay parser delegates the indented body to the PDL parser (see [Sub-language Pluggability](../sub-languages.md)).

The pages in this section document the full projection sub-language — every directive, operation, and expression the PDL supports. The same language is also used standalone by Cratis Chronicle to define projections without writing code; see [Chronicle projections](/chronicle/projections/projection-declaration-language/) for how Chronicle hosts and executes it.

## Syntax

```screenplay
projection <Name> => <ReadModel>
  <PDL grammar>
```

## Example

```screenplay
projection InvoiceDetails => InvoiceDetailsReadModel
  key invoiceId
  from InvoiceRegistered key invoiceId
    customerId    = customerId
    invoiceNumber = invoiceNumber
    currency      = currency
    status        = "draft"
    registeredAt  = $eventContext.occurred
  from InvoiceSent
    status = "sent"
    sentAt = sentAt
  from InvoicePaid
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

## The PDL vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `key <property>` | Which property identifies the read model instance. |
| `from <EventType>` | Maps event properties onto the read model. Same-named properties map automatically. |
| `join <property> on <key>` | Joins related state by a key property. |
| `children <collection> identified by <key>` | Projects events into a child collection. |
| `parent <property>` | Identifies the owning parent of a child. |
| `remove with <EventType>` | Removes the instance (or child) when the event occurs. |
| `increment` / `decrement <property>` | Counters maintained per event. |
| `clear <property>` | Removes the value the property holds. |
| `$eventContext.occurred` | The timestamp of the event being projected. |

An aggregating projection using counters:

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

Declare as many as the behavior needs. Each names its own read model, and [printing](../printing.md) writes them back out in the order they were declared. Splitting them across two slices would say the system has two behaviors where it has one.

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
- [Expressions](grammar.md#expressions) - Understanding expression syntax
- [Grammar (EBNF)](grammar.md) - Complete formal grammar specification
