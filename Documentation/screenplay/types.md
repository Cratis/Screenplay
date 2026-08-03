# Types

A [concept](concepts.md) names a single primitive value. A **type** names a *shape* — the structured child records real events routinely carry: the lines of an invoice, the answers of a questionnaire, the contact behind a billing address.

Without it, a document can reference `lines InvoiceLine[]` but never say what an `InvoiceLine` is. The reader is left guessing, and the concepts that only ever live inside such a record — a `PersonName`, an `EmailAddress` — have no reason to be declared at all, so the document understates the application's own compliance surface. A `type` closes both gaps.

## Syntax

```screenplay
type <Name>
  [description "<text>"]
  <property> <TypeRef>
  ...
```

A type declares at least one property. The property line is the same one events and commands use — a name, a type reference, and the optional `[]` and `?` suffixes — so everything you know about one applies to the other.

## Example

```screenplay
concept ProductName        : String
concept Quantity           : Int
concept Money              : Decimal
concept DiscountPercentage : Decimal

type InvoiceLine
  description "A single billed line of an invoice"
  lineNumber  Int
  productName ProductName
  quantity    Quantity
  unitPrice   Money
  discount    DiscountPercentage?
```

The type is then referenced by name from anywhere a type reference is allowed — an event property, a command property, or another type:

```screenplay
event InvoiceRegistered
  invoiceId InvoiceId
  lines     InvoiceLine[]
```

## Types compose

A type may reference another type, which is how a nested shape is expressed — by reference rather than by nesting the declaration:

```screenplay
concept PersonName   : String @pii
concept EmailAddress : String @pii

type BillingContact
  name  PersonName
  email EmailAddress

type Invoice
  contact BillingContact
  lines   InvoiceLine[]
```

## Where types earn their keep — compliance

This is the reason to reach for a type rather than leaving a shape undeclared. A `[PII]` value buried inside a child record is still personal data, and Chronicle still encrypts and erases it. But if the shape is never declared, nothing in the document says so — and a reader auditing the model sees a smaller PII surface than the application actually has.

Declaring `BillingContact` gives `PersonName` and `EmailAddress` a home in the document, so `@pii` and its [reason](concepts.md#why-a-value-is-personal-data) travel with them where a reader can see them.

## Unresolved references are reported

The compiler resolves every property type reference against the primitives, the declared concepts, the declared types and the imports. A reference to something the document never declares is a **warning**, not an error — a runtime may well resolve the name, but a document that silently depends on a shape living outside it is exactly what the warning is there to surface:

```text
Unknown type 'InvoiceLine' on 'lines' of event 'InvoiceRegistered' -
declare it with 'concept InvoiceLine : <Primitive>' or 'type InvoiceLine'
```

Concept and type names share one namespace — declaring both a `concept InvoiceLine` and a `type InvoiceLine` is an error.

## Type or concept?

| Use | When |
| --- | --- |
| `concept` | The value *is* a primitive with a domain meaning — an id, a name, an amount, a code. |
| `type` | The value is a shape made of several such values. |

Reach for a concept first. A type exists for the shapes a single primitive cannot express — and its properties should themselves be concepts, so the strong typing goes all the way down.
