# Modules, Features and Slices

## Module

The module is the top-level namespace and maps to a bounded context. One module per file is the convention but not enforced.

```screenplay
module <Name>
  [description "<text>"]

  [<screen templates>]
  [<dialog templates>]
  [<features>]
```

Screen templates and dialog templates declared at module level are described in [Layouts and templates](templates.md).

## Features

Features are vertical slice groupings. They nest arbitrarily deep for sub-features.

```screenplay
feature <Name>
  [description "<text>"]
  [feature <Name>]*   ← sub-features
  [slice <type> <Name>]+
```

## Slices

The slice is the atomic unit of behavior, aligned with Event Modeling. A slice has a type and a name, and contains the constructs that implement the behavior.

```screenplay
slice <SliceType> <Name>
  [description "<text>"]
  <constructs>
```

## Descriptions

Modules, features, slices, [personas](personas.md), and [commands](commands.md) take an optional `description` as their first body line — a human-readable summary consumers such as Prologue surface when presenting the model. At most one per declaration.

```screenplay
module Invoicing
  description "Everything related to invoicing customers"

  feature InvoiceManagement
    description "Registering and managing the lifecycle of invoices"

    slice StateChange RegisterInvoice
      description "Registers a new invoice"
```

When one line is not enough, use a fenced block — the same ``` convention as inline code blocks. The fenced text is kept verbatim:

````screenplay
module Invoicing
  description
    ```
    Everything related to invoicing customers.
    Registration, lifecycle and payment tracking of invoices.
    ```
````

### Slice types

| Type | Description |
| --- | --- |
| `StateChange` | A command → events flow; something that changes the system |
| `StateView` | A query + projection + screen; something that reads the system |
| `Automation` | A reaction or reducer; something that runs when something happens |
| `Translate` | A capture; converts external data into events |

### What goes in a slice

| Construct | Typical slice type | Page |
| --- | --- | --- |
| `event` | any | [Events](events.md) |
| `command` | `StateChange` | [Commands](commands.md) |
| `constraint` | `StateChange` | [Constraints](constraints.md) |
| `query` | `StateView` | [Queries](queries.md) |
| `readmodel` | `StateView` | [Read models](readmodels.md) |
| `projection` | `StateView` | [Projections](projections/index.md) |
| `reducer` | `StateView` | [Read models](readmodels.md#reducers) |
| `screen` | `StateView` | [Screens](screens.md) |
| `reaction` | `Automation` | [Reactions](reactions.md) |
| `capture` | `Translate` | [Captures](captures.md) |

Every one of these may appear as many times as the behavior needs — several events, several commands, [several projections](projections/index.md#several-projections-in-one-slice). Only `description` is limited to one. A slice is one behavior, not one artifact of each kind.

## Example

```screenplay
module Invoicing

  feature InvoiceManagement

    slice StateChange RegisterInvoice
      command RegisterInvoice
        ...
      event InvoiceRegistered
        ...

    slice StateView InvoiceList
      query ListInvoices => InvoiceListReadModel[]
      projection InvoiceList => InvoiceListReadModel
        ...
      screen InvoiceList
        ...
```
