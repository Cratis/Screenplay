# Projections

Projections declare how events are projected into a queryable read model. The body of a `projection` block is written in the **Projection Declaration Language (PDL)** — an embedded sub-grammar. The Screenplay parser delegates the indented body to the PDL parser (see [Sub-language Pluggability](sub-languages.md)).

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

## Full PDL grammar

The authoritative PDL grammar lives in the Chronicle documentation:
<https://www.cratis.io/chronicle/projections/projection-declaration-language/grammar/>
