# Modules, Features and Slices

## Module

The module is the top-level namespace and maps to a bounded context. One module per file is the convention but not enforced.

```screenplay
module <Name>

  [<layouts>]
  [<features>]
```

Layout templates declared at module level are described in [Screens](screens.md#layout-templates).

## Features

Features are vertical slice groupings. They nest arbitrarily deep for sub-features.

```screenplay
feature <Name>
  [feature <Name>]*   ← sub-features
  [slice <type> <Name>]+
```

## Slices

The slice is the atomic unit of behavior, aligned with Event Modeling. A slice has a type and a name, and contains the constructs that implement the behavior.

```screenplay
slice <SliceType> <Name>
  <constructs>
```

### Slice types

| Type | Description |
| --- | --- |
| `StateChange` | A command → events flow; something that changes the system |
| `StateView` | A query + projection + screen; something that reads the system |
| `Automation` | A reactor or reducer; something that reacts to events |
| `Translate` | A capture; converts external data into events |

### What goes in a slice

| Construct | Typical slice type | Page |
| --- | --- | --- |
| `event` | any | [Events](events.md) |
| `command` | `StateChange` | [Commands](commands.md) |
| `constraint` | `StateChange` | [Constraints](constraints.md) |
| `query` | `StateView` | [Queries](queries.md) |
| `projection` | `StateView` | [Projections](projections.md) |
| `screen` | `StateView` | [Screens](screens.md) |
| `reactor` | `Automation` | [Reactors](reactors.md) |
| `capture` | `Translate` | [Captures](captures.md) |

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
