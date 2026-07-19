# Queries

Queries are read-side entry points. A query maps to a return type — a read model, or a collection of read models with `[]`.

## Syntax

```screenplay
query <Name> => <ReturnType>[[]?]
  [by <paramName> <Type>]
  [filter <paramName> <Type>?]
  [authorize <PolicyName> [or <PolicyName>]*]
```

| Clause | Meaning |
| --- | --- |
| `by` | The identifying parameter — the query returns the instance it identifies. |
| `filter` | An optional parameter narrowing the result set. Filter types are typically optional (`?`). |
| `authorize` | The [policies](policies.md) that must pass. |

## Examples

A single-instance query identified by a parameter:

```screenplay
query GetInvoice => InvoiceDetailsReadModel
  by invoiceId InvoiceId
  authorize IsAuthenticated
```

A collection query with optional filters:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  filter status     InvoiceStatus?
  filter customerId CustomerId?
  authorize IsAuthenticated
```

## Guidance

- Name queries as descriptive reads: `GetInvoice`, `ListInvoices`, `GetOverdueInvoices`.
- The return type is a read model built by a [projection](projections/index.md) in the same or another `StateView` slice.
- [Screens](screens.md) bind to queries with `data <ReadModel> via query <QueryName>`.
