# Events

Events are immutable, past-tense facts — the record of something that happened. An event declaration defines the event type's name and properties. Properties use [concepts](concepts.md), composite [types](types.md), or primitives.

## Syntax

```screenplay
event <Name>
  [tag <value>]*
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

## Tags

`tag` lines attach tags to every append of the event — Chronicle stores them alongside the event so consumers can filter and group on them. A tag value is a bare identifier or string literal for static tags, or a `$context.` expression for tags resolved from context at append time.

```screenplay
event InvoiceRegistered
  tag invoicing
  tag "billing"
  tag $context.identity.id
  invoiceId InvoiceId
```

Tags can also be declared per production site — on a [`produces` block](commands.md#the-produces-block) and on a capture [`append` block](captures.md) — where they apply to that specific append rather than every occurrence of the event type.

## Guidance

- **Name events in the past tense** and make them self-describing: `InvoiceRegistered`, never `Created`.
- **One purpose per event.** If an event needs a nullable property to cover two situations, model the second situation as its own event.
- **Compliance is inherited.** A property typed with a `@pii` concept is PII — nothing extra to declare on the event.
- The event-source identity is not an event property; it travels in the event context. The command binds it with its [`identifier`](commands.md#the-identifier) property, and marking an event property `identifier` is an error.
- **Declare the shapes you reference.** A property typed `InvoiceLine[]` needs a [`type InvoiceLine`](types.md); the compiler warns when it resolves against nothing the document declares.
