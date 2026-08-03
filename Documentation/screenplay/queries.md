# Queries

Queries are read-side entry points. A query maps to a return type — a read model, or a collection of read models with `[]` — and says what it is for, where each of its parameters comes from, and, when it needs to, how it is performed.

## Syntax

```screenplay
query <Name> => <ReturnType>[[]?]
  [description "<text>"]
  [by <paramName> <Type> [from <source>]]
  [filter <paramName> <Type>? [from <source>]]
  [authorize <PolicyName> [or <PolicyName>]*]
  [performer
    file <Path>
    | csharp
        ```
        <C# returning the result>
        ```
    | sql
        ```
        <SQL returning the result>
        ```]
```

| Clause | Meaning |
| --- | --- |
| `description` | What the query is trying to accomplish, in prose. |
| `by` | The identifying parameter — the query returns the instance it identifies. |
| `filter` | An optional parameter narrowing the result set. Filter types are typically optional (`?`). |
| `from` | Fills the parameter from the query context instead of the caller. |
| `authorize` | The [policies](policies.md) that must pass. |
| `performer` | The code that performs the query — an external file, or an inline block. |

## Description

A query's `description` says what the read is *for*, in the words of the domain. It is the first body line, at most one per query, and takes either the quoted single-line form or a fenced block — the same shape as a [command description](commands.md#description):

```screenplay
query GetOverdueInvoices => OverdueInvoicesReadModel[]
  description "Invoices past their due date, oldest first, for the collections worklist"
  authorize IsAccountant
```

That sentence is what a reader — human or LLM — has to work from when generating or reviewing an implementation, so write the intent, not a restatement of the name.

## Parameters

`by` names the identifying parameter; `filter` narrows the result set. Both are supplied by the caller — a screen, an API client — unless they declare a source with `from`:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus?
  filter customerId CustomerId?
  filter tenantId   TenantId from $context.tenant
  authorize IsAuthenticated
```

`status` and `customerId` come from the UI. `tenantId` comes from the [query context](context.md) — the caller never chooses it, and the document says so once instead of every implementation remembering to. Any [mapping source](commands.md#mapping-sources) works as a `from` source, so `$env.` and constants are available too.

## Examples

A single-instance query identified by a parameter:

```screenplay
query GetInvoice => InvoiceDetailsReadModel
  description "One invoice with its lines, customer and shipping status"
  by invoiceId InvoiceId
  authorize IsAuthenticated
```

A collection query with optional filters:

```screenplay
query ListInvoices => InvoiceListReadModel[]
  description "Every invoice the caller may see, narrowed by status and customer"
  filter status     InvoiceStatus?
  filter customerId CustomerId?
  authorize IsAuthenticated
```

## The `performer` block

Most queries need nothing more than their return type — the read model a [projection](projections/index.md) already builds is the answer. When a query does need its own logic, `performer` is where it goes. It is the query's counterpart to a command's [handler](commands.md#the-handler-block), and it takes the same two shapes: a `file` reference, or an inline block.

Delegating to a file:

```screenplay
query GetInvoiceSummary => InvoiceSummaryReadModel
  description "The counters behind the dashboard header"
  performer
    file Queries/InvoiceSummaryPerformer.cs
```

Inline, in a top-level language:

````screenplay
query GetOverdueInvoices => OverdueInvoicesReadModel[]
  description "Invoices past their due date, oldest first"
  performer
    csharp
      ```
      return readModels
          .Where(invoice => invoice.Status == InvoiceStatus.Overdue)
          .Where(invoice => invoice.TenantId == context.Tenant)
          .OrderBy(invoice => invoice.DueDate);
      ```
````

Or in SQL, when the read is a query against a relational store:

````screenplay
query ListLineItems => InvoiceLineReportReadModel[]
  description "Every invoice line, priced, for the accounting line report"
  filter tenantId TenantId from $context.tenant
  performer
    sql
      ```
      select   InvoiceId, LineNumber, Quantity, UnitPrice
      from     InvoiceLineReport
      where    TenantId = @tenantId
      order by InvoiceId, LineNumber
      ```
````

Inside a performer, `context` is the [`QueryContext`](context.md) — the query's arguments, the tenant, the calling identity, the causation, and when the query was received. A `file` reference compiles against the same type, so moving a block out to a file changes nothing about what it can see.

A query is complete without a performer. The `performer` is realization metadata, not a precondition — see [Declarative first](grammar.md#declarative-first--file-is-never-required).

## Guidance

- Name queries as descriptive reads: `GetInvoice`, `ListInvoices`, `GetOverdueInvoices`.
- The return type is a read model built by a [projection](projections/index.md) in the same or another `StateView` slice.
- Anything the caller must not be able to choose — the tenant, the caller's own subject — belongs on a `from` parameter, not on a `filter` the UI supplies.
- [Screens](screens.md) bind to queries with `data <ReadModel> via query <QueryName>`.
