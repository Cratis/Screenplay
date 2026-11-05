---
title: "Cratis Screenplay types for structured domain data"
description: "Declare reusable record shapes in Cratis Screenplay with types, use concepts for individual domain values, and check nested specification data."
---

# Types

A Cratis Screenplay `type` declares a record with named properties, such as an invoice line or a billing contact. Use a [concept](concepts.md) for one domain value, such as a quantity or an email address; use a type when those values belong together in a record.

Declaring `InvoiceLine` tells readers what `lines InvoiceLine[]` contains. It also gives the compiler a shape to check when a specification supplies line values, including any nested records.

## Syntax

```screenplay
type <Name>
  [description "<text>"]
  [file <path>]
  <property> <TypeRef>
  ...
```

The optional `file` line points to the repository-relative implementation file. It lets readers navigate from the model to its code, but does not replace the property declarations. See [File references](file-references.md).

A type declares at least one property. Events, commands and read models use the same property order: name, type, then `optional` when the value may be absent. Write a collection as `InvoiceLine[] optional` when the collection itself may be absent; this does not make its items optional.

`optional` is case-sensitive and only acts as a modifier after a type. You can still name a property `optional` (`optional String`), name a type `optional` (`value optional`), or refer to that type optionally (`value optional optional`). A command identifier must remain required and scalar. Screen data bindings, behavior parameters and concept bases do not accept optionality. Optional reads are reserved for future support; they do not yet describe absence behavior.

### Compatibility note

The earlier `String?` and `InvoiceLine[]?` spellings remain valid with Information diagnostic `PLAY0479`; they do not fail `--warnaserror`. Write `String optional` and `InvoiceLine[] optional` in new models. [Editor quick fixes](vscode.md#optional-values-and-quick-fixes) and [workspace repairs](mcp/authoring-tools.md#fix-a-diagnostic) migrate the spelling without changing meaning, comments or alignment. Canonical printing writes `optional` except for [`query Q => observable?`](queries.md#observable-queries). That one spelling has no equivalent keyword form, so it stays unchanged, produces no `PLAY0479`, and is excluded from repairs and document migrations.

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
  discount    DiscountPercentage optional
```

Here, `Quantity` describes one integer value; `InvoiceLine` combines that quantity with a product name, price and optional discount. Reference the type by name from an event, command or another type. This event fragment assumes an `InvoiceId` concept is also declared or imported:

```screenplay
event InvoiceRegistered
  invoiceId InvoiceId
  lines     InvoiceLine[]
```

## Structured values in specifications

A type declaration describes the fields; a specification supplies their values. For a smaller order-line example, use this shape:

```screenplay
type InvoiceLine
  sku String
  quantity Int
```

If `PlaceOrder` declares `lines InvoiceLine[]`, write its specification value as a [structured value](structured-values.md): a `lines` header with one `item` block per invoice line. Use `lines none` for an empty list.

```screenplay
when PlaceOrder
  lines
    item
      sku      = "A-1"
      quantity = 2
```

The compiler checks `sku` and `quantity` against `InvoiceLine`, following nested declared types and checking enum members. An unknown member of a known shape is an error. If a shape is undeclared or only named by an external import, the compiler does not guess its members.

An enum is a concept, not a composite type. Its member names remain the values in specifications and conditions even when every member has an explicit number. The opt-in `enum-pinning` completeness check also reports its stored-identity hint for an unnumbered enum reached through a composite event property. See [Enum concepts](concepts.md#enum-concepts) for numbering rules and executable admission.

## Types compose

Keep each type declaration at the top level and refer to it by name to build a nested shape. In this example, `Invoice` contains one `BillingContact` and a list of `InvoiceLine` records:

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

## Read-model child shapes

Declare a `type` at document level, not inside a module, feature or slice. Use it for the reusable value shape of a [read-model member](readmodels.md#member-value-types), rather than referencing another read model:

```screenplay
type InvoiceLine
  lineNumber Int
  description String

module Billing
  feature Invoices
    slice StateView Details
      readmodel InvoiceDetails
        invoiceId Uuid key
        lines InvoiceLine[]
        selectedLine InvoiceLine optional
```

Both `lines` and `selectedLine` use `InvoiceLine` as a composite value type. Declaring `readmodel InvoiceLine` instead would define a view, not a reusable member type. Keep the `type` at the top level, including in an imported file; the diagnostics for slice-local declarations are explained below. A [children projection](projections/grammar.md#children-block) can populate `lines` without changing how its member type resolves.

## Where types earn their keep — compliance

A nested record can contain personally identifiable information (PII) just as an event's direct properties can. In `BillingContact`, the `PersonName` and `EmailAddress` concepts carry `@pii`, so a reader can follow the record to the personal values it contains.

Declare those concepts and their [reasons for being personal data](concepts.md#why-a-value-is-personal-data) rather than leaving the child shape outside the model. The declaration makes that data visible for review; it does not by itself configure an application's encryption or erasure.

## Unresolved references are reported

The compiler checks property type names against primitives, declared concepts, declared types and imports. An undeclared name produces a warning, so a document can compile while still depending on a shape it does not describe:

```text
Unknown type 'InvoiceLine' on 'lines' of event 'InvoiceRegistered' -
declare it with 'concept InvoiceLine : <Primitive>' or 'type InvoiceLine'
```

Read-model members have a further restriction: their types must be value types, not other read models. A type name that resolves only to a read model produces compile error `PLAY0273`. An undeclared name is not rejected at compile time, but fails binding for execution (`screenplay test`) with `PLAY0273`. A slice-local `type` is skipped with warning `PLAY0029`, so a member referencing it also fails binding. See [Member value types](readmodels.md#member-value-types).

Concept and type names share one namespace — declaring both a `concept InvoiceLine` and a `type InvoiceLine` is an error.

## Type or concept?

| Use | When |
| --- | --- |
| `concept` | The value *is* a primitive with a domain meaning — an id, a name, an amount, a code. |
| `type` | The value is a shape made of several such values. |

Use concepts where individual properties have domain meaning, and types to group them into records. Once the shape is declared, follow [Structured values](structured-values.md) to supply objects and lists in specifications, mappings and seeds.
