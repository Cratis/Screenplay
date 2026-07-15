# Events

Events are immutable, past-tense facts — the record of something that happened. An event declaration defines the event type's name and properties. Properties use [concepts](concepts.md) or primitives.

## Syntax

```screenplay
event <Name>
  <property> <Type>
  ...
```

## Example

```screenplay
event InvoiceRegistered
  invoiceId     InvoiceId
  customerId    CustomerId
  invoiceNumber InvoiceNumber
  lines         InvoiceLine[]
  currency      CurrencyCode
  registeredAt  DateTime
  status        InvoiceStatus
```

## Type modifiers

| Modifier | Syntax | Example |
| --- | --- | --- |
| Collection | `<Type>[]` | `lines InvoiceLine[]` |
| Optional | `<Type>?` | `note String?` |

## Guidance

- **Name events in the past tense** and make them self-describing: `InvoiceRegistered`, never `Created`.
- **One purpose per event.** If an event needs a nullable property to cover two situations, model the second situation as its own event.
- **Compliance is inherited.** A property typed with a `@pii` concept is PII — nothing extra to declare on the event.
- The event-source identity is not an event property; it travels in the event context. Commands bind it when they produce events.
